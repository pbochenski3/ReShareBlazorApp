using ReShareBlazorApp.Shared.DTOs;

namespace ReShareBlazorApp.Api.Interfaces.Services
{
    public interface IAuthService
    {
        Task<RegisterResponse?> RegisterAsync(RegisterRequest request);
        Task<LoginResponse?> LoginAsync(LoginRequest request);
        Task<RegisterResponse?> VerifyEmailAsync(VerifyEmailRequest request);
        Task<RegisterResponse?> ConfirmEmailByTokenAsync(string email, string token);
        Task<RegisterResponse?> ResendCodeAsync(string email);
    }
}
