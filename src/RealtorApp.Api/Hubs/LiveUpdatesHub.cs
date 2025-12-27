using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using RealtorApp.Contracts.Commands.Chat.Requests;
using RealtorApp.Contracts.Commands.Chat.Responses;
using RealtorApp.Domain.Interfaces;

namespace RealtorApp.Api.Hubs;

[Authorize]
public sealed class LiveUpdatesHub(IUserAuthService userAuthService, IChatService chatService, IUserService userService) : Hub
{
    private static readonly ConcurrentDictionary<long, HashSet<string>> _userConnections = new();
    private static readonly ConcurrentDictionary<long, SemaphoreSlim> _userConnectionLocks = new();
    private static readonly ConcurrentDictionary<long, HashSet<long>> _activeConversationUsers = new();
    private static readonly ConcurrentDictionary<long, SemaphoreSlim> _conversationLocks = new();
    private readonly IUserAuthService _userAuthService = userAuthService;
    private readonly IChatService _chatService = chatService;
    private readonly IUserService _userService = userService;

    private string _uuid => Context.UserIdentifier
        ?? throw new HubException("Unauthenticated: no UserIdentifier present.");

    private static class GroupNames
    {
        public static string Conversation(long conversationId) => $"conversation-{conversationId}";
        public static string LiveUpdates(long userId) => $"live-updates-{userId}";
    }

    public override async Task OnConnectedAsync()
    {
        var userId = await _userAuthService.GetUserIdByUuid(_uuid);

        if (userId == null)
        {
            return;
        }

        var semaphore = _userConnectionLocks.GetOrAdd((long)userId, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync();
        try
        {
            var set = _userConnections.GetOrAdd((long)userId, _ => new HashSet<string>());
            set.Add(Context.ConnectionId);
        }
        finally
        {
            semaphore.Release();
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? ex)
    {
        var userId = await _userAuthService.GetUserIdByUuid(_uuid);

        if (userId == null)
        {
            return;
        }

        SemaphoreSlim? lockToDispose = null;
        if (_userConnectionLocks.TryGetValue((long)userId, out var semaphore))
        {
            await semaphore.WaitAsync();
            try
            {
                if (_userConnections.TryGetValue((long)userId, out var set))
                {
                    set.Remove(Context.ConnectionId);
                    if (set.Count == 0)
                    {
                        _userConnections.TryRemove((long)userId, out _);
                        _userConnectionLocks.TryRemove((long)userId, out lockToDispose);
                    }
                }
            }
            finally
            {
                semaphore.Release();
            }

            lockToDispose?.Dispose();
        }

        await base.OnDisconnectedAsync(ex);
    }

    #region Conversations

    public async Task JoinConversation(long conversationId)
    {
        var userId = await _userAuthService.GetUserIdByUuid(_uuid);

        if (userId == null)
        {
            return;
        }

        if (!await _userAuthService.IsConversationParticipant((long)userId, conversationId))
            throw new HubException("Not a participant.");

        var semaphore = _conversationLocks.GetOrAdd(conversationId, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync();
        try
        {
            var activeUsers = _activeConversationUsers.GetOrAdd(conversationId, _ => new HashSet<long>());
            activeUsers.Add((long)userId);
        }
        finally
        {
            semaphore.Release();
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, GroupNames.Conversation(conversationId));
    }

    public async Task LeaveConversation(long conversationId)
    {
        var userId = await _userAuthService.GetUserIdByUuid(_uuid);

        await TryRemoveFromConversationDict(userId, conversationId);

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupNames.Conversation(conversationId));
    }

    private async Task TryRemoveFromConversationDict(long? userId, long conversationId)
    {
        SemaphoreSlim? lockToDispose = null;
        if (userId != null && _conversationLocks.TryGetValue(conversationId, out var semaphore))
        {
            await semaphore.WaitAsync();
            try
            {
                if (_activeConversationUsers.TryGetValue(conversationId, out var activeUsers))
                {
                    activeUsers.Remove((long)userId);
                    if (activeUsers.Count == 0)
                    {
                        _activeConversationUsers.TryRemove(conversationId, out _);
                        _conversationLocks.TryRemove(conversationId, out lockToDispose);
                    }
                }
            }
            finally
            {
                semaphore.Release();
            }

            lockToDispose?.Dispose();
        }
    }

    public async Task SendMessage(SendMessageCommand command)
    {
        var userId = await _userAuthService.GetUserIdByUuid(_uuid);

        if (userId == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(command.MessageText))
            throw new HubException("Invalid payload.");

        if (!await _userAuthService.IsConversationParticipant((long)userId, command.ConversationId))
            throw new HubException("Not a participant.");

        HashSet<long> activeUserIds = await SafeGetActiveUsersInConversation(command.ConversationId);
        var saved = await _chatService.SendMessageAsync(command, activeUserIds);

        if (saved == null || !string.IsNullOrEmpty(saved.ErrorMessage))
        {
            throw new HubException("Failed to send message.");
        }

        saved.LocalId = command.LocalId;

        await Clients.Group(GroupNames.Conversation(command.ConversationId))
            .SendAsync("onMessage", saved);

        await TryBroadcastLiveUpdate(command.SenderId, command.ConversationId, saved);
    }

    private async Task TryBroadcastLiveUpdate(long initiatingUserId, long listingId, SendMessageCommandResponse message)
    {
        var assignedUserIds = await _userAuthService.GetUsersAssignedToListing(listingId);

        HashSet<long> activeUserIds = await SafeGetActiveUsersInConversation(listingId);

        var liveUpdateRecipients = assignedUserIds
            .Except(activeUserIds)
            .Except([initiatingUserId])
            .Select(id => GroupNames.LiveUpdates(id))
            .ToList();

        if (liveUpdateRecipients.Count > 0)
        {
            await Clients.Groups(liveUpdateRecipients)
                .SendAsync("onConversationHasNewMessage", message);
        }
    }

    private async Task<HashSet<long>> SafeGetActiveUsersInConversation(long listingId)
    {
        HashSet<long> activeUserIds = [];
        if (_conversationLocks.TryGetValue(listingId, out var semaphore))
        {
            await semaphore.WaitAsync();
            try
            {
                if (_activeConversationUsers.TryGetValue(listingId, out var active))
                {
                    activeUserIds = [.. active];
                }
            }
            finally
            {
                semaphore.Release();
            }
        }

        return activeUserIds;
    }

    public async Task SetTyping(long conversationId, bool isTyping)
    {
        var userId = await _userAuthService.GetUserIdByUuid(_uuid);

        if (userId == null)
        {
            return;
        }

        var user = await _userService.GetUserProfileAsync((long)userId);

        if (!await _userAuthService.IsConversationParticipant((long)userId, conversationId) || user == null)
            throw new HubException("Not a participant.");

        await Clients.OthersInGroup(GroupNames.Conversation(conversationId))
            .SendAsync("onTyping", new { userId, name = user.FirstName + " " + user.LastName?.FirstOrDefault() , isTyping });
    }

    public async Task JoinLiveUpdatesGroup()
    {
        var userId = await _userAuthService.GetUserIdByUuid(_uuid);

        if (userId == null)
        {
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, GroupNames.LiveUpdates((long)userId));
    }

    public async Task LeaveLiveUpdatesGroup()
    {
        var userId = await _userAuthService.GetUserIdByUuid(_uuid);

        if (userId == null)
        {
            return;
        }

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupNames.LiveUpdates((long)userId));
    }

    #endregion
}
