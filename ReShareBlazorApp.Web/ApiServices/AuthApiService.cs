using ReShareBlazorApp.Shared.DTOs;
using ReShareBlazorApp.Web.Models;

namespace ReShareBlazorApp.Web.ApiServices
{
    public class AuthApiService
    {
        private readonly HttpClient _httpClient;
        public AuthApiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<LoginResponse?> LoginAsync(LoginModel model)
        {
            var request = new LoginRequest
            {
                Email = model.Email,
                Password = model.Password
            };
            var response = await _httpClient.PostAsJsonAsync(
                "api/auth/login",
                request
                );
            if (!response.IsSuccessStatusCode)
                return null;
            return await response.Content.ReadFromJsonAsync<LoginResponse>();
        }

        public async Task<RegisterResponse?> RegisterAsync(RegisterModel model)
        {
            var request = new RegisterRequest
            {
                UserName = model.Username,
                Email = model.Email,
                Password = model.Password,
                ConfirmPassword = model.ConfirmPassword
            };
            var response = await _httpClient.PostAsJsonAsync(
                "api/auth/register",
                request
                );

            return await response.Content.ReadFromJsonAsync<RegisterResponse>();
        }

        public async Task<RegisterResponse?> VerifyEmailAsync(string email, string code)
        {
            var request = new VerifyEmailRequest
            {
                Email = email,
                Code = code
            };
            var response = await _httpClient.PostAsJsonAsync(
                "api/auth/verify-email",
                request
                );
            return await response.Content.ReadFromJsonAsync<RegisterResponse>();
        }

        public async Task<RegisterResponse?> ConfirmEmailByLinkAsync(string email, string token)
        {
            var response = await _httpClient.GetAsync(
                $"api/auth/confirm-email?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}"
                );
            return await response.Content.ReadFromJsonAsync<RegisterResponse>();
        }

        public async Task<RegisterResponse?> ResendCodeAsync(string email)
        {
            var request = new ResendCodeRequest
            {
                Email = email
            };
            var response = await _httpClient.PostAsJsonAsync(
                "api/auth/resend-code",
                request
                );
            return await response.Content.ReadFromJsonAsync<RegisterResponse>();
        }
    }
}
