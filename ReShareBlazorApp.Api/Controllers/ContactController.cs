using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Mvc;
using ReShareBlazorApp.Shared.DTOs;

namespace ReShareBlazorApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ContactController : ControllerBase
{
    private readonly ReshareDbContext _context;
    private readonly ILogger<ContactController> _logger;
    private readonly IActivityLogger _activityLogger;
    private readonly IConfiguration _config;

    public ContactController(ReshareDbContext context, ILogger<ContactController> logger, IActivityLogger activityLogger)
    public ContactController(IConfiguration config)
    {
        _context = context;
        _logger = logger;
        _activityLogger = activityLogger;
        _config = config;
    }

    [HttpPost]
    public async Task<IActionResult> SendContactEmail([FromBody] ContactRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var smtpServer = _config["EmailSettings:SmtpServer"];
            var smtpPort = int.Parse(_config["EmailSettings:SmtpPort"] ?? "587");
            var senderEmail = _config["EmailSettings:SenderEmail"];
            var senderPassword = _config["EmailSettings:SenderPassword"];
            var receiverEmail = _config["EmailSettings:ReceiverEmail"];

            using var client = new SmtpClient(smtpServer, smtpPort)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(senderEmail, senderPassword)
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(senderEmail!, "ODDAJ DALEJ - Formularz"),
                Subject = $"Nowa wiadomość z formularza od: {request.Name}",
                Body = $"Od: {request.Name} ({request.Email})\n\nTreść wiadomości:\n{request.Message}",
                IsBodyHtml = false
            };

            mailMessage.To.Add(receiverEmail!);
            mailMessage.ReplyToList.Add(new MailAddress(request.Email));

            await client.SendMailAsync(mailMessage);

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
                Name = m.Name,
                Email = m.Email,
                Subject = m.Subject,
                Message = m.Message,
                CreatedAt = m.CreatedAt,
                IsRead = m.IsRead
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
            return StatusCode(500, $"Błąd wysyłania wiadomości: {ex.Message}");
        }
    }
}