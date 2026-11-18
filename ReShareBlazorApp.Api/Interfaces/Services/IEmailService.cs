namespace ReShareBlazorApp.Api.Interfaces.Services
{
    public interface IEmailService
    {
        Task SendVerificationEmailAsync(string toEmail, string code, string token);
    }
}
