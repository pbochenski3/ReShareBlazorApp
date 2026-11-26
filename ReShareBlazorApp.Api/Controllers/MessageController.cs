using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReShareBlazorApp.Api.Interfaces.Services;
using ReShareBlazorApp.Shared.DTOs;
using System.Security.Claims;

namespace ReShareBlazorApp.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/message")]
    public class MessageController : ControllerBase
    {
        private readonly IMessageService _messageService;

        public MessageController(IMessageService messageService)
        {
            _messageService = messageService;
        }

        [HttpGet("conversations")]
        public async Task<ActionResult<List<ConversationDTO>>> GetConversations()
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized();

            return Ok(await _messageService.GetConversationsAsync(userId));
        }

        [HttpGet("{conversationId:int}")]
        public async Task<IActionResult> GetMessages(int conversationId)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized();

            var result = await _messageService.GetMessagesAsync(conversationId, userId);
            if (result == null)
                return NotFound();

            return Ok(new { conversation = result.Value.Conversation, messages = result.Value.Messages });
        }

        [HttpPost("start")]
        public async Task<ActionResult<ConversationDTO>> StartConversation(StartConversationRequest request)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized();

            var conversation = await _messageService.StartConversationAsync(userId, request);
            if (conversation == null)
                return BadRequest("Nie udało się rozpocząć rozmowy.");

            return Ok(conversation);
        }

        [HttpPost("{conversationId:int}/send")]
        public async Task<ActionResult<MessageDTO>> SendMessage(int conversationId, SendMessageRequest request)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized();

            var message = await _messageService.SendMessageAsync(userId, conversationId, request.Content);
            if (message == null)
                return BadRequest("Nie udało się wysłać wiadomości.");

            return Ok(message);
        }

        // Administrator pisze do użytkownika, który zgłosił dane ogłoszenie (widok Zgłoszenia w panelu admina).
        [Authorize(Roles = "Admin")]
        [HttpPost("support/start")]
        public async Task<ActionResult<ConversationDTO>> StartSupportConversation(StartSupportConversationRequest request)
        {
            if (!TryGetUserId(out var adminId))
                return Unauthorized();

            var conversation = await _messageService.StartSupportConversationAsync(
                adminId, request.ReportId, request.Content);

            if (conversation == null)
                return BadRequest("Nie udało się wysłać wiadomości do zgłaszającego.");

            return Ok(conversation);
        }

        private bool TryGetUserId(out int userId) =>
            int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
    }
}
