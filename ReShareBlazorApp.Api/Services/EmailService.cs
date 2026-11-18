using ReShareBlazorApp.Api.Interfaces.Services;
using System.Net;
using System.Net.Mail;

namespace ReShareBlazorApp.Api.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendVerificationEmailAsync(string toEmail, string code, string token)
        {
            var frontendBaseUrl = _configuration["App:FrontendUrl"] ?? "https://localhost:7254";
            var confirmLink = $"{frontendBaseUrl}/confirm-email?token={Uri.EscapeDataString(token)}&email={Uri.EscapeDataString(toEmail)}";

            var subject = "Potwierdź swój adres e-mail – ODDAJ DALEJ";
            var body = $@"
<html>
<body style='font-family: Inter, Arial, sans-serif; color: #0f172a; max-width: 600px; margin: 0 auto;'>
    <div style='background: #097171; padding: 24px 32px; border-radius: 12px 12px 0 0;'>
        <h1 style='color: #fff; margin: 0; font-size: 1.4rem;'>🟢 ODDAJ DALEJ</h1>
    </div>
    <div style='padding: 32px; border: 1px solid #e3e8ea; border-top: none; border-radius: 0 0 12px 12px;'>
        <h2 style='margin-top: 0;'>Potwierdź swój adres e-mail</h2>
        <p>Dziękujemy za rejestrację! Aby aktywować konto, użyj jednej z poniższych metod:</p>
        
        <h3>Metoda 1 – Kod weryfikacyjny</h3>
        <p>Wpisz poniższy kod na stronie rejestracji:</p>
        <div style='background: #f4f6f7; border: 2px dashed #097171; border-radius: 12px; padding: 20px; text-align: center; margin: 16px 0;'>
            <span style='font-size: 2rem; font-weight: 700; letter-spacing: 8px; color: #097171;'>{code}</span>
        </div>
        
        <h3>Metoda 2 – Link potwierdzający</h3>
        <p>Kliknij poniższy przycisk:</p>
        <div style='text-align: center; margin: 24px 0;'>
            <a href='{confirmLink}' style='display: inline-block; padding: 14px 36px; background: #097171; color: #fff; text-decoration: none; border-radius: 999px; font-weight: 600; font-size: 1rem;'>
                Potwierdź e-mail
            </a>
        </div>
        
        <p style='color: #5b6678; font-size: 0.85rem;'>Kod i link wygasają po 15 minutach. Jeśli nie rejestrowałeś się na naszej stronie, zignoruj tę wiadomość.</p>
    </div>
</body>
</html>";

            // Próba wysłania e-maila przez SMTP
            var smtpHost = _configuration["Smtp:Host"];
            if (!string.IsNullOrEmpty(smtpHost))
            {
                try
                {
                    var smtpPort = int.Parse(_configuration["Smtp:Port"] ?? "587");
                    var smtpUser = _configuration["Smtp:User"] ?? "";
                    var smtpPass = _configuration["Smtp:Password"] ?? "";
                    var fromEmail = _configuration["Smtp:From"] ?? smtpUser;

                    using var client = new SmtpClient(smtpHost, smtpPort)
                    {
                        Credentials = new NetworkCredential(smtpUser, smtpPass),
                        EnableSsl = true
                    };

                    var message = new MailMessage(fromEmail, toEmail, subject, body)
                    {
                        IsBodyHtml = true
                    };

                    await client.SendMailAsync(message);
                    _logger.LogInformation("Verification email sent to {Email}", toEmail);
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send email via SMTP, falling back to console log");
                }
            }

            // Fallback: logowanie do konsoli (na potrzeby developmentu)
            _logger.LogInformation(
                "========== VERIFICATION EMAIL ==========\n" +
                "To: {Email}\n" +
                "Code: {Code}\n" +
                "Token: {Token}\n" +
                "Link: {Link}\n" +
                "=========================================",
                toEmail, code, token, confirmLink);
        }
    }
}
