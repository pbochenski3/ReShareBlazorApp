using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReShareBlazorApp.Api.Date;
using ReShareBlazorApp.Api.Interfaces.Services;
using ReShareBlazorApp.Shared;
using ReShareBlazorApp.Shared.DTOs;

namespace ReShareBlazorApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ContactController : ControllerBase
{
    private readonly ReshareDbContext _context;
    private readonly ILogger<ContactController> _logger;
    private readonly IActivityLogger _activityLogger;
    private readonly IChatService _chatService;

    public ContactController(
        ReshareDbContext context,
        ILogger<ContactController> logger,
        IActivityLogger activityLogger,
        IChatService chatService)
    {
        _context = context;
        _logger = logger;
        _activityLogger = activityLogger;
        _chatService = chatService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateMessage([FromBody] ContactRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            int? userId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var parsedId)
                ? parsedId
                : null;

            var entity = new ContactMessage
            {
                UserId = userId,
                Name = request.Name.Trim(),
                Email = request.Email.Trim(),
                Subject = request.Subject.Trim(),
                Message = request.Message.Trim(),
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };

            _context.ContactMessages.Add(entity);
            await _context.SaveChangesAsync();

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd wysyłania wiadomości kontaktowej.");
            return StatusCode(500, "Nie udało się wysłać wiadomości.");
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<ActionResult<List<ContactMessageDTO>>> GetMessages()
    {
        var list = await _context.ContactMessages
            .OrderByDescending(m => m.CreatedAt)
            .Select(m => new ContactMessageDTO
            {
                Id = m.Id,
                UserId = m.UserId,
                Name = m.Name,
                Email = m.Email,
                Subject = m.Subject,
                Message = m.Message,
                CreatedAt = m.CreatedAt,
                IsRead = m.IsRead,
                IsReplied = m.IsReplied,
                RepliedAt = m.RepliedAt
            })
            .ToListAsync();

        return Ok(list);
    }

    [Authorize(Roles = "Admin")]
    [HttpPatch("{id:int}/read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var msg = await _context.ContactMessages.FindAsync(id);
        if (msg is null)
            return NotFound();

        msg.IsRead = true;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("{id:int}/thread")]
    public async Task<ActionResult<List<ChatMessageDTO>>> GetThread(int id)
    {
        return Ok(await _chatService.GetContactThreadAsync(id));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("{id:int}/reply")]
    public async Task<ActionResult<AdminReplyResultDTO>> ReplyToMessage(
        int id,
        [FromBody] AdminContactReplyRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var adminUserId))
            return Unauthorized();

        var result = await _chatService.SendAdminContactReplyAsync(User, adminUserId, id, request);
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteMessage(int id)
    {
        var msg = await _context.ContactMessages.FindAsync(id);
        if (msg is null)
            return NotFound();

        var subject = msg.Subject;

        _context.ContactMessages.Remove(msg);
        await _context.SaveChangesAsync();

        await _activityLogger.LogAsync(User, ActivityActions.AdminMessageDeleted,
            $"Usunięto wiadomość kontaktową (id: {id}, temat: {subject})",
            "ContactMessage", id);

        return NoContent();
    }
}
