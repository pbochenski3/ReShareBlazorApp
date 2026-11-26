using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReShareBlazorApp.Api.Date;
using ReShareBlazorApp.Api.Interfaces.Services;
using ReShareBlazorApp.Shared.DTOs;
using System.Security.Claims;

namespace ReShareBlazorApp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MessageController : ControllerBase
    {
        private readonly IMessageService _messageService;
        private readonly ReshareDbContext _context;
        private readonly ILogger<MessageController> _logger;

        public MessageController(
            IMessageService messageService,
            ReshareDbContext context,
            ILogger<MessageController> logger)
        {
            _messageService = messageService;
            _context = context;
            _logger = logger;
        }

        private int? GetCurrentUserId()
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(idClaim, out var id) ? id : null;
        }

        [Authorize]
        [HttpGet("conversations")]
        public async Task<ActionResult<List<ConversationSummaryDTO>>> GetConversations()
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
                return Unauthorized();

            var conversations = await _messageService.GetConversationsAsync(currentUserId.Value);
            return Ok(conversations);
        }

        [Authorize]
        [HttpGet("conversation/{otherUserId:int}")]
        public async Task<ActionResult<List<ChatMessageDTO>>> GetConversation(int otherUserId, [FromQuery] int? productId = null)
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
                return Unauthorized();

            var messages = await _messageService.GetConversationMessagesAsync(currentUserId.Value, otherUserId, productId);
            return Ok(messages);
        }

        [Authorize]
        [HttpPost("send")]
        public async Task<ActionResult<ChatMessageDTO>> SendMessage([FromBody] SendMessageRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
                return Unauthorized();

            var result = await _messageService.SendMessageAsync(currentUserId.Value, request);
            if (result == null)
                return BadRequest("Nie udało się wysłać wiadomości.");

            return Ok(result);
        }

        [Authorize]
        [HttpPost("mark-read/{otherUserId:int}")]
        public async Task<IActionResult> MarkAsRead(int otherUserId)
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
                return Unauthorized();

            await _messageService.MarkConversationAsReadAsync(currentUserId.Value, otherUserId);
            return NoContent();
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("logs")]
        public async Task<ActionResult<List<MessageLog>>> GetMessageLogs([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            var logs = await _context.MessageLogs
                .OrderByDescending(l => l.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(logs);
        }
    }
}
