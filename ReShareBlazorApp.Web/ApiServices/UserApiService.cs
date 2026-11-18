using Microsoft.AspNetCore.Components.Authorization;
using ReShareBlazorApp.Shared.DTOs;
using System.Net;
using System.Net.Http.Headers;

namespace ReShareBlazorApp.Web.ApiServices
{
    public class UserApiService
    {
        private readonly HttpClient _httpClient;
        private readonly AuthenticationStateProvider _authStateProvider;
        public UserApiService(HttpClient httpClient, AuthenticationStateProvider authStateProvider)
        {
            _httpClient = httpClient;
            _authStateProvider = authStateProvider;
        }

        public async Task<bool> CheckIfUserExistsAsync(string username)
        {
            var result = await _httpClient.GetFromJsonAsync<bool>
                ($"api/user?username={Uri.EscapeDataString(username)}");
            return result;
        }

        // Publiczny profil; null gdy użytkownik nie istnieje.
        public async Task<UserProfileDTO?> GetProfileAsync(string username)
        {
            var response = await _httpClient.GetAsync(
                $"api/user/profile/{Uri.EscapeDataString(username)}");
            if (response.StatusCode == HttpStatusCode.NotFound) return null;
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<UserProfileDTO>();
        }

        public async Task<UserProfileDTO?> GetMyProfileAsync()
        {
            using var request = await CreateRequestAsync(HttpMethod.Get, "api/user/me");
            if (request == null) return null;
            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<UserProfileDTO>();
        }

        // Zwraca null gdy OK, w przeciwnym razie komunikat błędu.
        public async Task<string?> UpdateProfileAsync(UpdateProfileRequest model)
        {
            using var request = await CreateRequestAsync(HttpMethod.Put, "api/user/me");
            if (request == null) return "Brak tokenu uwierzytelniającego!";
            request.Content = JsonContent.Create(model);
            var response = await _httpClient.SendAsync(request);
            return await ReadErrorAsync(response);
        }

        public async Task<string?> UploadAvatarAsync(byte[] bytes, string fileName, string contentType)
        {
            using var request = await CreateRequestAsync(HttpMethod.Post, "api/user/me/avatar");
            if (request == null) return "Brak tokenu uwierzytelniającego!";
            var fileContent = new ByteArrayContent(bytes);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            var form = new MultipartFormDataContent { { fileContent, "file", fileName } };
            request.Content = form;
            var response = await _httpClient.SendAsync(request);
            return await ReadErrorAsync(response);
        }

        public async Task<string?> DeleteAvatarAsync()
        {
            using var request = await CreateRequestAsync(HttpMethod.Delete, "api/user/me/avatar");
            if (request == null) return "Brak tokenu uwierzytelniającego!";
            var response = await _httpClient.SendAsync(request);
            return await ReadErrorAsync(response);
        }

        // Zdjęcia są serwowane przez aplikację Web (/avatars/...), która pobiera je z API.
        public string? GetAvatarUrl(string? relativePath)
        {
            return string.IsNullOrEmpty(relativePath) ? null : relativePath;
        }

        private async Task<HttpRequestMessage?> CreateRequestAsync(HttpMethod method, string url)
        {
            var authState = await _authStateProvider.GetAuthenticationStateAsync();
            var token = authState.User.FindFirst("access_token")?.Value;
            if (string.IsNullOrEmpty(token)) return null;
            var request = new HttpRequestMessage(method, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return request;
        }

        private static async Task<string?> ReadErrorAsync(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode) return null;
            if (response.StatusCode == HttpStatusCode.Unauthorized)
                return "Sesja wygasła – zaloguj się ponownie.";
            try
            {
                var body = await response.Content.ReadFromJsonAsync<ErrorBody>();
                if (!string.IsNullOrEmpty(body?.Message)) return body.Message;
            }
            catch { }
            return "Nie udało się zapisać zmian.";
        }

        private class ErrorBody { public string? Message { get; set; } }
    }
}
