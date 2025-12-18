using System.Collections.Concurrent;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using RealtorApp.Contracts.Commands.Chat.Requests;
using RealtorApp.Domain.Interfaces;

namespace RealtorApp.Api.Hubs;

[Authorize]
public sealed class ChatHub(IUserAuthService userAuthService, IChatService chatService, IUserService userService) : Hub
{
    private static readonly ConcurrentDictionary<long, HashSet<string>> _userConnections = new();
    private readonly IUserAuthService _userAuthService = userAuthService;
    private readonly IChatService _chatService = chatService;
    private readonly IUserService _userService = userService;

    private string _uuid => Context.UserIdentifier
        ?? throw new HubException("Unauthenticated: no UserIdentifier present.");

    public override async Task OnConnectedAsync()
    {
        var userId = await _userAuthService.GetUserIdByUuid(_uuid);

        if (userId == null)
        {
            return;
        }

        var set = _userConnections.GetOrAdd((long)userId, _ => new HashSet<string>());
        lock (set) set.Add(Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? ex)
    {
        var userId = await _userAuthService.GetUserIdByUuid(_uuid);

        if (userId == null)
        {
            return;
        }

        if (_userConnections.TryGetValue((long)userId, out var set))
        {
            lock (set) set.Remove(Context.ConnectionId);
            if (set.Count == 0) _userConnections.TryRemove((long)userId, out _);
        }
        await base.OnDisconnectedAsync(ex);
    }

    public async Task JoinConversation(long conversationId)
    {
        var userId = await _userAuthService.GetUserIdByUuid(_uuid);

        if (userId == null)
        {
            return;
        }

        if (!await _userAuthService.IsConversationParticipant((long)userId, conversationId))
            throw new HubException("Not a participant.");

        await Groups.AddToGroupAsync(Context.ConnectionId, conversationId.ToString());
    }

    public Task LeaveConversation(long conversationId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, conversationId.ToString());

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

        // Persist + produce canonical server payload
        var saved = await _chatService.SendMessageAsync(command);

        if (saved == null || !string.IsNullOrEmpty(saved.ErrorMessage))
        {
            throw new HubException("Failed to send message.");
        }

        saved.LocalId = command.LocalId;

        // Echo to all current members of the conversation (including sender)
        await Clients.Group(command.ConversationId.ToString())
            .SendAsync("onMessage", saved);
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

        await Clients.OthersInGroup(conversationId.ToString())
            .SendAsync("onTyping", new { userId, name = user.FirstName + " " + user.LastName?.FirstOrDefault() , isTyping });
    }
}
