using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Mvc;
using ReShareBlazorApp.Shared.DTOs;

namespace ReShareBlazorApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ContactController : ControllerBase
{
    private readonly IConfiguration _config;

    public ContactController(IConfiguration config)
    {
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
            HttpContext.RequestServices
                .GetRequiredService<ILogger<ContactController>>()
                .LogError(ex, "Błąd wysyłania wiadomości kontaktowej.");
            return StatusCode(500, "Nie udało się wysłać wiadomości.");
        }
    }
}