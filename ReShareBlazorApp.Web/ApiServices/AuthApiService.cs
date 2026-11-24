using ReShareBlazorApp.Shared.DTOs;
using ReShareBlazorApp.Web.Models;

namespace ReShareBlazorApp.Web.ApiServices;

public class AuthApiService
{
    private readonly HttpClient _httpClient;

    public AuthApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<LoginResponse?> LoginAsync(LoginModel model)
    {
        var (response, _) = await LoginWithDetailsAsync(model);
        return response;
    }

    public async Task<(LoginResponse? Response, string? ErrorMessage)> LoginWithDetailsAsync(LoginModel model)
    {
        var request = new LoginRequest { Email = model.Email, Password = model.Password };

        try
        {
            using var response = await _httpClient.PostAsJsonAsync("api/auth/login", request);
            if (!response.IsSuccessStatusCode)
            {
                try
                {
                    var error = await response.Content.ReadFromJsonAsync<LoginErrorBody>();
                    if (!string.IsNullOrWhiteSpace(error?.Message))
                        return (null, error.Message);
                }
                catch (System.Text.Json.JsonException) { }

                return (null, "Nieprawidłowy e-mail lub hasło!");
            }

            return (await response.Content.ReadFromJsonAsync<LoginResponse>(), null);
        }
        catch (HttpRequestException)
        {
            return (null, "Nie można połączyć się z serwerem API. Sprawdź, czy projekt Api jest uruchomiony.");
        }
    }

    public async Task LogoutAsync(string? token)
    {
        if (string.IsNullOrEmpty(token))
            return;

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "api/auth/logout");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            using var response = await _httpClient.SendAsync(request);
        }
        catch (HttpRequestException) { }
        catch (TaskCanceledException) { }
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

        using var response = await _httpClient.PostAsJsonAsync("api/auth/register", request);
        return await response.Content.ReadFromJsonAsync<RegisterResponse>();
    }

    public async Task<RegisterResponse?> VerifyRegistrationAsync(string email, string code)
    {
        using var response = await _httpClient.PostAsJsonAsync(
            "api/auth/verify-registration",
            new VerifyRegistrationRequest { Email = email, Code = code });

        return await response.Content.ReadFromJsonAsync<RegisterResponse>();
    }

    public async Task<bool> ForgotPasswordAsync(string email)
    {
        try
        {
            using var response = await _httpClient.PostAsJsonAsync(
                "api/auth/forgot-password",
                new ForgotPasswordRequest { Email = email });
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException)
        {
            return false;
        }
    }

    public async Task<string?> ResetPasswordAsync(ResetPasswordRequest request)
    {
        try
        {
            using var response = await _httpClient.PostAsJsonAsync("api/auth/reset-password", request);
            if (response.IsSuccessStatusCode)
                return null;

            try
            {
                var error = await response.Content.ReadFromJsonAsync<ErrorBody>();
                if (!string.IsNullOrWhiteSpace(error?.Message))
                    return error.Message;
            }
            catch (System.Text.Json.JsonException) { }

            return "Nie udało się zmienić hasła. Spróbuj ponownie.";
        }
        catch (HttpRequestException)
        {
            return "Nie udało się połączyć z serwerem. Spróbuj ponownie później.";
        }
    }

    private sealed class LoginErrorBody
    {
        public string? Message { get; set; }
    }

    private sealed class ErrorBody
    {
        public string? Message { get; set; }
    }
}
