using Microsoft.AspNetCore.Components.Authorization;
using ReShareBlazorApp.Shared.DTOs;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace ReShareBlazorApp.Web.ApiServices
{
    public class AdminSupportApiService
    {
        private readonly HttpClient _httpClient;
        private readonly AuthenticationStateProvider _authStateProvider;

        public AdminSupportApiService(HttpClient httpClient, AuthenticationStateProvider authStateProvider)
        {
            _httpClient = httpClient;
            _authStateProvider = authStateProvider;
        }

        private async Task<HttpRequestMessage> CreateRequestAsync(HttpMethod method, string url)
        {
            var authState = await _authStateProvider.GetAuthenticationStateAsync();
            var token = authState.User.FindFirst("access_token")?.Value;

            var request = new HttpRequestMessage(method, url);
            if (!string.IsNullOrEmpty(token))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return request;
        }

        public async Task<List<ConversationDTO>?> GetSupportConversationsAsync()
        {
            try
            {
                using var request = await CreateRequestAsync(HttpMethod.Get, "api/admin/support/conversations");
                using var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode) return null;
                return await response.Content.ReadFromJsonAsync<List<ConversationDTO>>();
            }
            catch (HttpRequestException) { return null; }
        }

        public Task<List<AdminReplyDTO>> GetContactRepliesAsync(int contactId) =>
            GetRepliesAsync($"api/admin/support/contact/{contactId}/replies");

        public Task<(bool Success, string? Error, AdminReplyDTO? Reply)> ReplyToContactAsync(int contactId, string message) =>
            PostReplyAsync($"api/admin/support/contact/{contactId}/reply", message);

        public Task<List<AdminReplyDTO>> GetReportRepliesAsync(int reportId) =>
            GetRepliesAsync($"api/admin/support/report/{reportId}/replies");

        public Task<(bool Success, string? Error, AdminReplyDTO? Reply)> ReplyToReportAsync(int reportId, string message) =>
            PostReplyAsync($"api/admin/support/report/{reportId}/reply", message);

        public async Task<ChatLogPageDTO?> GetChatLogsAsync(int skip, int take, int? userId)
        {
            try
            {
                var url = $"api/admin/support/chat-logs?skip={skip}&take={take}";
                if (userId.HasValue)
                    url += $"&userId={userId.Value}";

                using var request = await CreateRequestAsync(HttpMethod.Get, url);
                using var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode) return null;
                return await response.Content.ReadFromJsonAsync<ChatLogPageDTO>();
            }
            catch (HttpRequestException) { return null; }
        }

        public async Task<string?> GetChatLogContentAsync(int logId)
        {
            try
            {
                using var request = await CreateRequestAsync(HttpMethod.Get, $"api/admin/support/chat-logs/{logId}/content");
                using var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode) return null;
                var dto = await response.Content.ReadFromJsonAsync<ChatLogContentDTO>();
                return dto?.Content;
            }
            catch (HttpRequestException) { return null; }
        }

        private async Task<List<AdminReplyDTO>> GetRepliesAsync(string url)
        {
            try
            {
                using var request = await CreateRequestAsync(HttpMethod.Get, url);
                using var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode) return new();
                return await response.Content.ReadFromJsonAsync<List<AdminReplyDTO>>() ?? new();
            }
            catch (HttpRequestException) { return new(); }
        }

        private async Task<(bool Success, string? Error, AdminReplyDTO? Reply)> PostReplyAsync(string url, string message)
        {
            try
            {
                using var request = await CreateRequestAsync(HttpMethod.Post, url);
                request.Content = JsonContent.Create(new AdminReplyRequest { Message = message });

                using var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                    return (false, await ApiErrors.ReadMessageAsync(response) ?? "Nie udało się wysłać odpowiedzi.", null);

                var reply = await response.Content.ReadFromJsonAsync<AdminReplyDTO>();
                return (true, null, reply);
            }
            catch (HttpRequestException)
            {
                return (false, "Nie można połączyć się z serwerem API.", null);
            }
        }
    }
}