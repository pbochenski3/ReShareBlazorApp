using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ReShareBlazorApp.Api.Date;
using ReShareBlazorApp.Api.Interfaces.Services;
using ReShareBlazorApp.Shared.DTOs;

namespace ReShareBlazorApp.Api.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly IChatService _chatService;
    private readonly ReshareDbContext _context;

    public ChatHub(IChatService chatService, ReshareDbContext context)
    {
        _chatService = chatService;
        _context = context;
    }

    public override async Task OnConnectedAsync()
    {
        if (!TryGetUserId(out var userId))
        {
            Context.Abort();
            return;
        }

        var user = await _context.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new { u.IsActive, u.LockoutEnd, u.IsDeleted })
            .FirstOrDefaultAsync();

        if (user is null || user.IsDeleted || !user.IsActive ||
            (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow))
        {
            Context.Abort();
            return;
        }

        await base.OnConnectedAsync();
    }

    public async Task<ChatMessageDTO?> SendMessage(SendChatMessageRequest request)
    {
        if (!TryGetUserId(out var senderId))
            throw new HubException("Brak autoryzacji.");

        var (message, error) = await _chatService.SendMessageAsync(senderId, request);
        if (error is not null || message is null)
            throw new HubException(error ?? "Nie udało się wysłać wiadomości.");

        return message;
    }

    public async Task MarkConversationRead(int conversationId)
    {
        if (!TryGetUserId(out var userId))
            return;

        await _chatService.MarkConversationAsReadAsync(userId, conversationId);
    }

    public async Task NotifyTyping(int conversationId, int recipientId)
    {
        if (!TryGetUserId(out var senderId) || recipientId == senderId)
            return;

        var isParticipant = await _context.ChatConversations
            .AsNoTracking()
            .AnyAsync(c => c.Id == conversationId && (c.User1Id == senderId || c.User2Id == senderId));

        if (!isParticipant)
            return;

        var senderName = Context.User?.FindFirstValue(ClaimTypes.Name) ?? "Użytkownik";
        await Clients.User(recipientId.ToString())
            .SendAsync("UserTyping", conversationId, senderId, senderName);
    }

    private bool TryGetUserId(out int userId)
    {
        var claim = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? Context.User?.FindFirstValue("sub");
        return int.TryParse(claim, out userId);
    }
}
