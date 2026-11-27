using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReShareBlazorApp.Api.Date;
using ReShareBlazorApp.Api.Interfaces.Services;
using ReShareBlazorApp.Shared;
using ReShareBlazorApp.Shared.DTOs;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;

namespace ReShareBlazorApp.Api.Controllers
{
    [ApiController]
    [Route("api/admin/support")]
    [Authorize(Roles = "Admin")]
    public class AdminSupportController : ControllerBase
    {
        private readonly ReshareDbContext _context;
        private readonly IChatService _chatService;
        private readonly IMessageEncryptionService _encryption;
        private readonly IAppEmailSender _emailSender;
        private readonly IActivityLogger _activityLogger;
        private readonly ILogger<AdminSupportController> _logger;

        public AdminSupportController(
            ReshareDbContext context,
            IChatService chatService,
            IMessageEncryptionService encryption,
            IAppEmailSender emailSender,
            IActivityLogger activityLogger,
            ILogger<AdminSupportController> logger)
        {
            _context = context;
            _chatService = chatService;
            _encryption = encryption;
            _emailSender = emailSender;
            _activityLogger = activityLogger;
            _logger = logger;
        }

        private int CurrentAdminId =>
            int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

        private string CurrentAdminName => User.FindFirstValue(ClaimTypes.Name) ?? "admin";

        // ---------- Rozmowy wsparcia ----------

        [HttpGet("conversations")]
        public async Task<ActionResult<List<ConversationDTO>>> GetSupportConversations()
        {
            return Ok(await _chatService.GetSupportConversationsAsync());
        }

        // ---------- Szybki kontakt: odpowiedź na czacie (albo e-mailem, gdy nadawca nie ma konta) ----------

        [HttpGet("contact/{id:int}/replies")]
        public async Task<ActionResult<List<AdminReplyDTO>>> GetContactReplies(int id)
        {
            var replies = await _context.AdminReplies
                .AsNoTracking()
                .Where(r => r.ContactMessageId == id)
                .OrderBy(r => r.CreatedAt)
                .ToListAsync();

            return Ok(replies.Select(ToDto).ToList());
        }

        [HttpPost("contact/{id:int}/reply")]
        public async Task<IActionResult> ReplyToContact(int id, [FromBody] AdminReplyRequest request)
        {
            var text = request.Message.Trim();
            if (text.Length == 0)
                return BadRequest(new { message = "Treść odpowiedzi jest wymagana." });

            var contact = await _context.ContactMessages.FirstOrDefaultAsync(m => m.Id == id);
            if (contact is null)
                return NotFound();

            var subject = SanitizeSubject(contact.Subject);

            // E-mail jest potwierdzany przy rejestracji, więc konto z tym adresem należy do nadawcy.
            var normalizedEmail = contact.Email.Trim().ToUpperInvariant();
            var accountId = await _context.Users
                .AsNoTracking()
                .Where(u => u.NormalizedEmail == normalizedEmail && u.IsActive)
                .Select(u => (int?)u.Id)
                .FirstOrDefaultAsync();

            string channel;

            if (accountId.HasValue)
            {
                // Nadawca ma konto: odpowiedź trafia na jego czat (rozmowa z administracją).
                try
                {
                    var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
                    var chatText = $"Odpowiedź na Twoją wiadomość „{subject}”:\n\n{text}";
                    await _chatService.SendSupportMessageToUserAsync(CurrentAdminId, accountId.Value, chatText, ip);
                }
                catch (ChatException ex)
                {
                    return BadRequest(new { message = ex.Message });
                }

                channel = "InApp";
            }
            else
            {
                // Nadawca nie ma konta, więc nie ma jak odpisać na czacie - wysyłamy e-mail.
                var emailSubject = subject.StartsWith("Re:", StringComparison.OrdinalIgnoreCase)
                    ? subject
                    : $"Re: {subject}";

                try
                {
                    await _emailSender.SendAsync(
                        contact.Email,
                        emailSubject,
                        BuildReplyEmail(contact.Name, text, subject, contact.Message));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Nie udało się wysłać odpowiedzi na wiadomość kontaktową {Id}.", id);
                    return StatusCode(502, new { message = "Nadawca nie ma konta, a wysłanie e-maila się nie udało. Sprawdź konfigurację SMTP." });
                }

                channel = "Email";
            }

            var reply = new AdminReply
            {
                ContactMessageId = contact.Id,
                AdminId = CurrentAdminId,
                AdminName = CurrentAdminName,
                RecipientUserId = accountId,
                RecipientEmail = contact.Email,
                Channel = channel,
                EncryptedBody = _encryption.Encrypt(text, EncryptionPurposes.AdminReply),
                CreatedAt = DateTime.UtcNow
            };
            _context.AdminReplies.Add(reply);
            contact.IsRead = true;
            await _context.SaveChangesAsync();

            await _activityLogger.LogAsync(User, ActivityActions.AdminContactReplied,
                $"Odpowiedziano na wiadomość kontaktową (id: {id}, kanał: {(channel == "InApp" ? "czat" : "e-mail")})",
                "ContactMessage", id);

            return Ok(ToDto(reply));
        }

        // ---------- Zgłoszenia: odpowiedź w serwisie (czat) ----------

        [HttpGet("report/{id:int}/replies")]
        public async Task<ActionResult<List<AdminReplyDTO>>> GetReportReplies(int id)
        {
            var replies = await _context.AdminReplies
                .AsNoTracking()
                .Where(r => r.ReportId == id)
                .OrderBy(r => r.CreatedAt)
                .ToListAsync();

            return Ok(replies.Select(ToDto).ToList());
        }

        [HttpPost("report/{id:int}/reply")]
        public async Task<IActionResult> ReplyToReport(int id, [FromBody] AdminReplyRequest request)
        {
            var text = request.Message.Trim();
            if (text.Length == 0)
                return BadRequest(new { message = "Treść odpowiedzi jest wymagana." });

            var report = await _context.Reports
                .AsNoTracking()
                .Where(r => r.Id == id)
                .Select(r => new { r.Id, r.UserId, ProductName = r.Product.Name })
                .FirstOrDefaultAsync();

            if (report is null)
                return NotFound();

            var productName = report.ProductName.Length > 100
                ? report.ProductName[..100] + "…"
                : report.ProductName;

            var fullText = $"Odpowiedź na Twoje zgłoszenie dotyczące ogłoszenia „{productName}”:\n\n{text}";

            try
            {
                var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
                await _chatService.SendSupportMessageToUserAsync(CurrentAdminId, report.UserId, fullText, ip);
            }
            catch (ChatException ex)
            {
                return BadRequest(new { message = ex.Message });
            }

            var reply = new AdminReply
            {
                ReportId = report.Id,
                AdminId = CurrentAdminId,
                AdminName = CurrentAdminName,
                RecipientUserId = report.UserId,
                Channel = "InApp",
                EncryptedBody = _encryption.Encrypt(text, EncryptionPurposes.AdminReply),
                CreatedAt = DateTime.UtcNow
            };
            _context.AdminReplies.Add(reply);
            await _context.SaveChangesAsync();

            await _activityLogger.LogAsync(User, ActivityActions.AdminReportReplied,
                $"Odpowiedziano na zgłoszenie (id: {id})",
                "Report", id);

            return Ok(ToDto(reply));
        }

        // ---------- Logi wiadomości (zaszyfrowane) ----------

        [HttpGet("chat-logs")]
        public async Task<ActionResult<ChatLogPageDTO>> GetChatLogs(
            [FromQuery] int skip = 0, [FromQuery] int take = 50, [FromQuery] int? userId = null)
        {
            skip = Math.Max(skip, 0);
            take = Math.Clamp(take, 1, 200);

            IQueryable<ChatMessageLog> query = _context.ChatMessageLogs.AsNoTracking();
            if (userId.HasValue)
            {
                var uid = userId.Value;
                query = query.Where(l => l.SenderId == uid || l.RecipientId == uid);
            }

            var total = await query.CountAsync();

            var items = await query
                .OrderByDescending(l => l.Id)
                .Skip(skip)
                .Take(take)
                .Select(l => new
                {
                    l.Id,
                    l.MessageId,
                    l.ConversationId,
                    l.SenderId,
                    l.RecipientId,
                    l.Action,
                    l.IpAddress,
                    l.CreatedAt
                })
                .ToListAsync();

            var userIds = items.Select(i => i.SenderId)
                .Concat(items.Where(i => i.RecipientId.HasValue).Select(i => i.RecipientId!.Value))
                .Distinct()
                .ToList();

            var names = await _context.Users
                .AsNoTracking()
                .Where(u => userIds.Contains(u.Id))
                .Select(u => new { u.Id, u.UserName })
                .ToDictionaryAsync(u => u.Id, u => u.UserName ?? string.Empty);

            return Ok(new ChatLogPageDTO
            {
                Total = total,
                Items = items.Select(i => new ChatLogDTO
                {
                    Id = i.Id,
                    MessageId = i.MessageId,
                    ConversationId = i.ConversationId,
                    SenderId = i.SenderId,
                    SenderName = names.GetValueOrDefault(i.SenderId, $"#{i.SenderId}"),
                    RecipientId = i.RecipientId,
                    RecipientName = i.RecipientId.HasValue
                        ? names.GetValueOrDefault(i.RecipientId.Value, $"#{i.RecipientId.Value}")
                        : "Administracja",
                    Action = i.Action,
                    IpAddress = i.IpAddress,
                    CreatedAt = DateTime.SpecifyKind(i.CreatedAt, DateTimeKind.Utc)
                }).ToList()
            });
        }

        [HttpGet("chat-logs/{id:int}/content")]
        public async Task<ActionResult<ChatLogContentDTO>> GetChatLogContent(int id)
        {
            var log = await _context.ChatMessageLogs.AsNoTracking().FirstOrDefaultAsync(l => l.Id == id);
            if (log is null)
                return NotFound();

            string content;
            try
            {
                content = _encryption.Decrypt(log.EncryptedContent, EncryptionPurposes.Log);
            }
            catch (Exception ex) when (ex is CryptographicException or FormatException)
            {
                _logger.LogError(ex, "Nie udało się odszyfrować logu {Id}.", id);
                return StatusCode(500, new { message = "Nie można odszyfrować tego wpisu (zmieniony klucz?)." });
            }

            await _activityLogger.LogAsync(User, ActivityActions.AdminChatLogViewed,
                $"Odszyfrowano log wiadomości (id: {id}, rozmowa: {log.ConversationId})",
                "ChatMessageLog", id);

            return Ok(new ChatLogContentDTO { Content = content });
        }

        // ---------- Pomocnicze ----------

        private AdminReplyDTO ToDto(AdminReply reply)
        {
            string body;
            try
            {
                body = _encryption.Decrypt(reply.EncryptedBody, EncryptionPurposes.AdminReply);
            }
            catch (Exception ex) when (ex is CryptographicException or FormatException)
            {
                body = "[nie można odszyfrować]";
            }

            return new AdminReplyDTO
            {
                Id = reply.Id,
                AdminName = reply.AdminName,
                Channel = reply.Channel,
                Recipient = reply.RecipientEmail ?? $"użytkownik #{reply.RecipientUserId}",
                Body = body,
                CreatedAt = DateTime.SpecifyKind(reply.CreatedAt, DateTimeKind.Utc)
            };
        }

        private static string SanitizeSubject(string subject)
        {
            var clean = subject.Replace("\r", " ").Replace("\n", " ").Trim();
            if (clean.Length == 0)
                clean = "Twoja wiadomość";
            return clean.Length > 150 ? clean[..150] : clean;
        }

        private static string BuildReplyEmail(string recipientName, string reply, string originalSubject, string originalMessage)
        {
            static string Html(string value) =>
                WebUtility.HtmlEncode(value).Replace("\r\n", "<br/>").Replace("\n", "<br/>");

            return $@"<div style=""font-family:Arial,sans-serif;max-width:600px;margin:0 auto;color:#0f172a;"">
  <h2 style=""color:#097171;margin:0 0 16px;"">ODDAJ DALEJ</h2>
  <p>Cześć <strong>{Html(recipientName)}</strong>,</p>
  <p style=""line-height:1.6;"">{Html(reply)}</p>
  <hr style=""border:none;border-top:1px solid #e3e8ea;margin:24px 0;"" />
  <p style=""font-size:13px;color:#64748b;margin:0 0 6px;"">Twoja wiadomość: <strong>{Html(originalSubject)}</strong></p>
  <p style=""font-size:13px;color:#64748b;line-height:1.5;margin:0;"">{Html(originalMessage)}</p>
</div>";
        }
    }
}