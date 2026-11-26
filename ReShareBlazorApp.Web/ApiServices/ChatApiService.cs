using Microsoft.AspNetCore.Components.Authorization;
using ReShareBlazorApp.Shared.DTOs;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace ReShareBlazorApp.Web.ApiServices
{
    public class ChatApiService
    {
        private readonly HttpClient _httpClient;
        private readonly AuthenticationStateProvider _authStateProvider;

        public ChatApiService(HttpClient httpClient, AuthenticationStateProvider authStateProvider)
        {
            _httpClient = httpClient;
            _authStateProvider = authStateProvider;
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

        public async Task<List<ConversationSummaryDTO>?> GetConversationsAsync()
        {
            using var request = await CreateRequestAsync(HttpMethod.Get, "api/message/conversations");
            if (request == null)
                return null;

            try
            {
                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                    return null;

                return await response.Content.ReadFromJsonAsync<List<ConversationSummaryDTO>>()
                       ?? new List<ConversationSummaryDTO>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<List<ChatMessageDTO>?> GetConversationAsync(int otherUserId, int? productId = null)
        {
            var url = $"api/message/conversation/{otherUserId}";
            if (productId.HasValue)
                url += $"?productId={productId.Value}";

            using var request = await CreateRequestAsync(HttpMethod.Get, url);
            if (request == null)
                return null;

            try
            {
                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                    return null;

                return await response.Content.ReadFromJsonAsync<List<ChatMessageDTO>>()
                       ?? new List<ChatMessageDTO>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<ChatMessageDTO?> SendMessageAsync(SendMessageRequest sendRequest)
        {
            using var request = await CreateRequestAsync(HttpMethod.Post, "api/message/send");
            if (request == null)
                return null;

            request.Content = JsonContent.Create(sendRequest);

            try
            {
                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                    return null;

                return await response.Content.ReadFromJsonAsync<ChatMessageDTO>();
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<bool> MarkAsReadAsync(int otherUserId)
        {
            using var request = await CreateRequestAsync(HttpMethod.Post, $"api/message/mark-read/{otherUserId}");
            if (request == null)
                return false;

            try
            {
                var response = await _httpClient.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch (HttpRequestException)
            {
                return false;
            }
        }
    }
}
