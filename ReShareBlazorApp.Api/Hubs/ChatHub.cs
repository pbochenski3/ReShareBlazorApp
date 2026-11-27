using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using ReShareBlazorApp.Api.Services;
using System.Security.Claims;

namespace ReShareBlazorApp.Api.Hubs;

[Authorize]
public sealed class ChatHub : Hub
{
    private readonly ChatService _chat;
    public ChatHub(ChatService chat) => _chat = chat;

    public async Task JoinConversation(int conversationId)
    {
        var userId = GetUserId();
        if (!await _chat.IsParticipantAsync(conversationId, userId))
            throw new HubException("Brak dostępu do tej rozmowy.");
        await Groups.AddToGroupAsync(Context.ConnectionId, $"conversation:{conversationId}");
    }

    public Task LeaveConversation(int conversationId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, $"conversation:{conversationId}");

    private int GetUserId()
    {
        var value = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(value, out var id)) throw new HubException("Nieprawidłowa sesja.");
        return id;
    }
}
