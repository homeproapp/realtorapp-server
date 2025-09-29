using System.Collections.Concurrent;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using RealtorApp.Domain.Interfaces;

namespace RealtorApp.Api.Hubs;



[Authorize]
public sealed class ChatHub(IUserAuthService userAuthService) : Hub
{
    private static readonly ConcurrentDictionary<long, HashSet<string>> _userConnections = new();
    private readonly IUserAuthService _userAuthService = userAuthService;

    // Convenience accessor for downstream code
    private Guid _uuid => Guid.Parse(Context.UserIdentifier!
        ?? throw new HubException("Unauthenticated: no UserIdentifier present."));

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

        // Optional: verify participant before joining
        if (!await _userAuthService.IsConversationParticipant(conversationId, (long)userId))
            throw new HubException("Not a participant.");

        await Groups.AddToGroupAsync(Context.ConnectionId, conversationId.ToString());
    }

    public Task LeaveConversation(long conversationId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, conversationId.ToString());

    // TODO: fix this method
    // public async Task SendMessage(dynamic dto) 
    // {
    //     // Defense-in-depth
    //     if (string.IsNullOrWhiteSpace(dto.ConversationId) || string.IsNullOrWhiteSpace(dto.Text))
    //         throw new HubException("Invalid payload.");

    //     if (!await _repo.IsParticipantAsync(dto.ConversationId, _userId))
    //         throw new HubException("Not a participant.");

    //     // Persist + produce canonical server payload
    //     var saved = await _repo.SaveMessageAsync(dto.ConversationId, _userId, dto.Text, dto.ClientTempId);

    //     // Echo to all current members of the conversation (including sender)
    //     await Clients.Group(dto.ConversationId)
    //         .SendAsync("onMessage", saved);
    // }

    public async Task SetTyping(long conversationId, bool isTyping)
    {
        var userId = await _userAuthService.GetUserIdByUuid(_uuid);

        if (userId == null)
        {
            return;
        }

        if (!await _userAuthService.IsConversationParticipant(conversationId, (long)userId))
            throw new HubException("Not a participant.");

        await Clients.OthersInGroup(conversationId.ToString())
            .SendAsync("onTyping", new { conversationId, userId, isTyping });
    }
}