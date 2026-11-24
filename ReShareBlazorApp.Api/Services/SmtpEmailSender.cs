using System.Net;
using System.Net.Mail;
using ReShareBlazorApp.Api.Interfaces.Services;

namespace ReShareBlazorApp.Api.Services
{
    public class SmtpEmailSender : IAppEmailSender
    {
        private readonly IConfiguration _config;
        private readonly IHostEnvironment _environment;
        private readonly ILogger<SmtpEmailSender> _logger;

        public SmtpEmailSender(
            IConfiguration config,
            IHostEnvironment environment,
            ILogger<SmtpEmailSender> logger)
        {
            _config = config;
            _environment = environment;
            _logger = logger;
        }

        public async Task SendAsync(string to, string subject, string htmlBody)
        {
            var server = _config["EmailSettings:SmtpServer"];
            var port = int.TryParse(_config["EmailSettings:SmtpPort"], out var p) ? p : 587;
            var sender = _config["EmailSettings:SenderEmail"];
            var password = _config["EmailSettings:SenderPassword"];

            var notConfigured =
                string.IsNullOrWhiteSpace(server) ||
                string.IsNullOrWhiteSpace(sender) ||
                string.IsNullOrWhiteSpace(password) ||
                sender.Contains("twoj.email") ||
                password.StartsWith("twoje-");

            if (notConfigured)
            {
                if (_environment.IsDevelopment())
                {
                    // W Development treść jest logowana pomocniczo, ale rejestracja i tak
                    // nie może zostać potwierdzona bez poprawnie skonfigurowanego SMTP.
                    _logger.LogWarning(
                        "SMTP nie jest skonfigurowany. Mail do {To}, temat: {Subject}\n{Body}",
                        to, subject, htmlBody);
                }
                else
                {
                    _logger.LogError("SMTP nie jest skonfigurowany - nie wysłano maila do {To}.", to);
                }

                throw new InvalidOperationException("SMTP nie jest skonfigurowany.");
            }

            using var client = new SmtpClient(server, port)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(sender, password)
            };

            using var message = new MailMessage
            {
                From = new MailAddress(sender, "ODDAJ DALEJ"),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };
            message.To.Add(to);

            await client.SendMailAsync(message);
        }
    }
}