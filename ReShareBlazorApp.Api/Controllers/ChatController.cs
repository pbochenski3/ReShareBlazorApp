using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReShareBlazorApp.Api.Interfaces.Services;
using ReShareBlazorApp.Shared.DTOs;
using System.Security.Claims;

namespace ReShareBlazorApp.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/chat")]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;

        public ChatController(IChatService chatService)
        {
            _chatService = chatService;
        }

        private bool TryGetUserId(out int userId) =>
            int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);

        [HttpGet("conversations")]
        public async Task<ActionResult<List<ConversationDTO>>> GetConversations()
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized();

            return Ok(await _chatService.GetConversationsAsync(userId));
        }

        [HttpGet("conversations/{id:int}")]
        public async Task<ActionResult<ConversationDTO>> GetConversation(int id)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized();

            var conversation = await _chatService.GetConversationAsync(userId, User.IsInRole("Admin"), id);
            return conversation is null ? NotFound() : Ok(conversation);
        }

        [HttpPost("conversations")]
        public async Task<ActionResult<ConversationDTO>> StartConversation([FromBody] StartConversationRequest request)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized();

            try
            {
                var conversation = await _chatService.StartConversationAsync(userId, request.RecipientId, request.ProductId);
                return Ok(conversation);
            }
            catch (ChatException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("conversations/{id:int}/messages")]
        public async Task<ActionResult<List<ChatMessageDTO>>> GetMessages(
            int id, [FromQuery] int? beforeId, [FromQuery] int take = 50)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized();

            var messages = await _chatService.GetMessagesAsync(userId, User.IsInRole("Admin"), id, beforeId, take);
            return messages is null ? NotFound() : Ok(messages);
        }

        [HttpPost("conversations/{id:int}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized();

            var ok = await _chatService.MarkAsReadAsync(userId, User.IsInRole("Admin"), id);
            return ok ? NoContent() : NotFound();
        }
    }
}