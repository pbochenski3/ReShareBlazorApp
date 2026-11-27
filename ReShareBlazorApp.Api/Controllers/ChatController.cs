using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ReShareBlazorApp.Api.Date;
using ReShareBlazorApp.Api.Hubs;
using ReShareBlazorApp.Api.Services;
using ReShareBlazorApp.Shared.DTOs;
using System.Security.Claims;

namespace ReShareBlazorApp.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/chat")]
public sealed class ChatController : ControllerBase
{
    private readonly ReshareDbContext _db;
    private readonly ChatService _chat;
    private readonly IHubContext<ChatHub> _hub;

    public ChatController(ReshareDbContext db, ChatService chat, IHubContext<ChatHub> hub)
    { _db = db; _chat = chat; _hub = hub; }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("conversations")]
    public async Task<ActionResult<List<ChatConversationDto>>> Conversations(CancellationToken ct) =>
        Ok(await _chat.GetConversationsAsync(CurrentUserId, ct));

    [HttpPost("conversations")]
    public async Task<IActionResult> CreateConversation(CreateConversationRequest request, CancellationToken ct)
    {
        if (request.OtherUserId == CurrentUserId) return BadRequest(new { message = "Nie możesz rozpocząć rozmowy z samym sobą." });
        try
        {
            var id = await _chat.GetOrCreateConversationAsync(CurrentUserId, request.OtherUserId, ct: ct);
            return Ok(new { conversationId = id });
        }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("conversations/{id:int}/messages")]
    public async Task<IActionResult> Messages(int id, CancellationToken ct)
    {
        try { return Ok(await _chat.GetMessagesAsync(id, CurrentUserId, ct)); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    [HttpPost("conversations/{id:int}/messages")]
    public async Task<IActionResult> Send(int id, SendChatMessageRequest request, CancellationToken ct)
    {
        try
        {
            var message = await _chat.SendAsync(id, CurrentUserId, request.Text, request.ReplyToMessageId, ct);
            await _hub.Clients.Group($"conversation:{id}").SendAsync("MessageReceived",
                new ChatMessagePushedDto { ConversationId = id, Message = message }, ct);
            return Ok(message);
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("conversations/{id:int}/read")]
    public async Task<IActionResult> MarkRead(int id, CancellationToken ct)
    {
        try { await _chat.MarkReadAsync(id, CurrentUserId, ct); return NoContent(); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    // Administrator odpowiada zgłaszającemu w osobnej rozmowie wsparcia 1:1.
    [Authorize(Roles = "Admin")]
    [HttpPost("admin/reports/{reportId:int}")]
    public async Task<IActionResult> OpenReportConversation(int reportId, CancellationToken ct)
    {
        var report = await _db.Reports.AsNoTracking().FirstOrDefaultAsync(r => r.Id == reportId, ct);
        if (report == null) return NotFound();
        try
        {
            var id = await _chat.GetOrCreateConversationAsync(CurrentUserId, report.UserId, true,
                $"report:{reportId}", ct);
            return Ok(new { conversationId = id });
        }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    // Formularz kontaktowy jest anonimowy; jeśli e-mail odpowiada istniejącemu kontu,
    // admin może otworzyć z nim oddzielną rozmowę. Nie ma dopasowania po samej nazwie.
    [Authorize(Roles = "Admin")]
    [HttpPost("admin/contact/{contactMessageId:int}")]
    public async Task<IActionResult> OpenContactConversation(int contactMessageId, CancellationToken ct)
    {
        var contact = await _db.ContactMessages.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == contactMessageId, ct);
        if (contact == null) return NotFound();
        if (!contact.UserId.HasValue)
            return BadRequest(new { message = "Ta wiadomość kontaktowa została wysłana anonimowo. Nie można bezpiecznie połączyć jej z kontem użytkownika." });
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == contact.UserId.Value && u.IsActive && !u.IsDeleted, ct);
        if (user == null) return BadRequest(new { message = "Konto użytkownika nie jest już dostępne." });
        try
        {
            var id = await _chat.GetOrCreateConversationAsync(CurrentUserId, user.Id, true,
                $"contact:{contactMessageId}", ct);
            return Ok(new { conversationId = id });
        }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }
}
