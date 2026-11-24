using Microsoft.AspNetCore.Components.Authorization;
using ReShareBlazorApp.Shared.DTOs;
using System.Net.Http.Headers;

namespace ReShareBlazorApp.Web.ApiServices
{
    public class NotificationApiService
    {
        private readonly HttpClient _httpClient;
        private readonly AuthenticationStateProvider _authStateProvider;

        public event Action? NotificationsChanged;

        public void NotifyNotificationsChanged() => NotificationsChanged?.Invoke();

        public NotificationApiService(
            HttpClient httpClient,
            AuthenticationStateProvider authStateProvider)
        {
            _httpClient = httpClient;
            _authStateProvider = authStateProvider;
        }

        public async Task<List<NotificationDTO>> GetNotificationsAsync()
        {
            using var request = await CreateRequestAsync(HttpMethod.Get, "api/notification");
            if (request is null)
                return new List<NotificationDTO>();

            using var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
                return new List<NotificationDTO>();

            return await response.Content.ReadFromJsonAsync<List<NotificationDTO>>()
                ?? new List<NotificationDTO>();
        }

        public async Task<bool> MarkAsReadAsync(int notificationId)
        {
            using var request = await CreateRequestAsync(
                HttpMethod.Put,
                $"api/notification/{notificationId}/read");
            if (request is null)
                return false;

            using var response = await _httpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
                NotifyNotificationsChanged();
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> MarkAllAsReadAsync()
        {
            using var request = await CreateRequestAsync(HttpMethod.Put, "api/notification/read-all");
            if (request is null)
                return false;

            using var response = await _httpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
                NotifyNotificationsChanged();
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteAsync(int notificationId)
        {
            using var request = await CreateRequestAsync(
                HttpMethod.Delete,
                $"api/notification/{notificationId}");
            if (request is null)
                return false;

            using var response = await _httpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
                NotifyNotificationsChanged();
            return response.IsSuccessStatusCode;
        }

        private async Task<HttpRequestMessage?> CreateRequestAsync(HttpMethod method, string url)
        {
            var authState = await _authStateProvider.GetAuthenticationStateAsync();
            var token = authState.User.FindFirst("access_token")?.Value;
            if (string.IsNullOrEmpty(token))
                return null;

            var request = new HttpRequestMessage(method, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return request;
        }
    }
}
