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
            var (resp, _) = await LoginWithDetailsAsync(model);
            return resp;
        }

        public async Task<(LoginResponse? Response, string? ErrorMessage)> LoginWithDetailsAsync(LoginModel model)
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
            {
                try
                {
                    var err = await response.Content.ReadFromJsonAsync<LoginErrorBody>();
                    if (!string.IsNullOrWhiteSpace(err?.Message))
                        return (null, err.Message);
                }
                catch { }

                return (null, "Nieprawidłowy e-mail lub hasło!");
            }
            var loginResp = await response.Content.ReadFromJsonAsync<LoginResponse>();
            return (loginResp, null);
        }

        private class LoginErrorBody
        {
            public string? Message { get; set; }
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
    }
}
