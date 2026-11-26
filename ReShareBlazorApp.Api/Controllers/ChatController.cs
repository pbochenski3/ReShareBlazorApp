using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReShareBlazorApp.Api.Interfaces.Services;
using ReShareBlazorApp.Shared.DTOs;

namespace ReShareBlazorApp.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;

    public ChatController(IChatService chatService)
    {
        _chatService = chatService;
    }

    [HttpGet("conversations")]
    public async Task<ActionResult<List<ChatConversationDTO>>> GetConversations()
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        return Ok(await _chatService.GetConversationsAsync(userId));
    }

    [HttpPost("conversations/start")]
    public async Task<ActionResult<ChatConversationDTO>> StartConversation([FromBody] StartConversationRequest request)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var (conversation, error) = await _chatService.GetOrCreateConversationAsync(
            userId, request.RecipientId, request.ProductId);

        if (error is not null || conversation is null)
            return BadRequest(new { message = error ?? "Nie udało się rozpocząć rozmowy." });

        return Ok(conversation);
    }

    [HttpGet("conversations/{conversationId:int}/messages")]
    public async Task<ActionResult<List<ChatMessageDTO>>> GetMessages(int conversationId)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var (messages, error) = await _chatService.GetMessagesAsync(userId, conversationId);
        if (error is not null || messages is null)
            return NotFound(new { message = error });

        return Ok(messages);
    }

    [HttpPost("messages")]
    public async Task<ActionResult<ChatMessageDTO>> SendMessage([FromBody] SendChatMessageRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var (message, error) = await _chatService.SendMessageAsync(userId, request);
        if (error is not null || message is null)
            return BadRequest(new { message = error });

        return Ok(message);
    }

    [HttpPost("conversations/{conversationId:int}/read")]
    public async Task<IActionResult> MarkConversationRead(int conversationId)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var count = await _chatService.MarkConversationAsReadAsync(userId, conversationId);
        return Ok(new { marked = count });
    }

    [HttpGet("unread-count")]
    public async Task<ActionResult<int>> GetUnreadCount()
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        return Ok(await _chatService.GetUnreadCountAsync(userId));
    }

    [HttpGet("users/search")]
    public async Task<ActionResult<List<ChatUserLookupDTO>>> SearchUsers([FromQuery] string? q = null)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        return Ok(await _chatService.SearchUsersAsync(userId, q));
    }

    private bool TryGetUserId(out int userId)
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");
        return int.TryParse(claim, out userId);
    }
}
