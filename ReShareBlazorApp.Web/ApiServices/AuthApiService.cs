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
            HttpResponseMessage response;
            try
            {
                response = await _httpClient.PostAsJsonAsync(
                    "api/auth/login",
                    request
                    );
            }
            catch (HttpRequestException)
            {
                return (null, "Nie można połączyć się z serwerem API. Sprawdź, czy projekt Api jest uruchomiony.");
            }
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

        // Zapisuje wylogowanie w dzienniku aktywności (nie przerywa wylogowania, gdy API nie odpowiada).
        public async Task LogoutAsync(string? token)
        {
            if (string.IsNullOrEmpty(token))
                return;

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, "api/auth/logout");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                (await _httpClient.SendAsync(request)).Dispose();
            }
            catch (HttpRequestException) { }
            catch (TaskCanceledException) { }
        }

        private class LoginErrorBody
        {
            public string? Message { get; set; }
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
    }
}
