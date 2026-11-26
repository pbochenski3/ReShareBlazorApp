using Microsoft.AspNetCore.Components.Authorization;
using ReShareBlazorApp.Shared.DTOs;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace ReShareBlazorApp.Web.ApiServices
{
    public class MessageApiService
    {
        private readonly HttpClient _httpClient;
        private readonly AuthenticationStateProvider _authStateProvider;

        public MessageApiService(HttpClient httpClient, AuthenticationStateProvider authStateProvider)
        {
            _httpClient = httpClient;
            _authStateProvider = authStateProvider;
        }

        public async Task<List<ConversationDTO>> GetConversationsAsync()
        {
            using var request = await CreateRequestAsync(HttpMethod.Get, "api/message/conversations");
            if (request is null)
                return new List<ConversationDTO>();

            using var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
                return new List<ConversationDTO>();

            return await response.Content.ReadFromJsonAsync<List<ConversationDTO>>()
                ?? new List<ConversationDTO>();
        }

        public async Task<(ConversationDTO? Conversation, List<MessageDTO> Messages)> GetMessagesAsync(int conversationId)
        {
            using var request = await CreateRequestAsync(HttpMethod.Get, $"api/message/{conversationId}");
            if (request is null)
                return (null, new List<MessageDTO>());

            using var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
                return (null, new List<MessageDTO>());

            var payload = await response.Content.ReadFromJsonAsync<ConversationMessagesPayload>();
            return (payload?.Conversation, payload?.Messages ?? new List<MessageDTO>());
        }

        public async Task<ConversationDTO?> StartConversationAsync(int recipientId, int productId, string content)
        {
            using var request = await CreateRequestAsync(HttpMethod.Post, "api/message/start");
            if (request is null)
                return null;

            request.Content = JsonContent.Create(new StartConversationRequest
            {
                RecipientId = recipientId,
                ProductId = productId,
                Content = content
            });

            using var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadFromJsonAsync<ConversationDTO>();
        }

        public async Task<MessageDTO?> SendMessageAsync(int conversationId, string content)
        {
            using var request = await CreateRequestAsync(HttpMethod.Post, $"api/message/{conversationId}/send");
            if (request is null)
                return null;

            request.Content = JsonContent.Create(new SendMessageRequest { Content = content });

            using var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadFromJsonAsync<MessageDTO>();
        }

        // Administrator pisze z poziomu panelu zgłoszeń do użytkownika, który złożył zgłoszenie.
        public async Task<ConversationDTO?> StartSupportConversationAsync(int reportId, string content)
        {
            using var request = await CreateRequestAsync(HttpMethod.Post, "api/message/support/start");
            if (request is null)
                return null;

            request.Content = JsonContent.Create(new StartSupportConversationRequest
            {
                ReportId = reportId,
                Content = content
            });

            using var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadFromJsonAsync<ConversationDTO>();
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

        private sealed class ConversationMessagesPayload
        {
            public ConversationDTO? Conversation { get; set; }
            public List<MessageDTO> Messages { get; set; } = new();
        }
    }
}
