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

        private async Task<HttpRequestMessage> CreateRequestAsync(HttpMethod method, string url)
        {
            var authState = await _authStateProvider.GetAuthenticationStateAsync();
            var token = authState.User.FindFirst("access_token")?.Value;

            var request = new HttpRequestMessage(method, url);
            if (!string.IsNullOrEmpty(token))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return request;
        }

        public async Task<List<ConversationDTO>?> GetConversationsAsync()
        {
            try
            {
                using var request = await CreateRequestAsync(HttpMethod.Get, "api/chat/conversations");
                using var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode) return null;
                return await response.Content.ReadFromJsonAsync<List<ConversationDTO>>();
            }
            catch (HttpRequestException) { return null; }
        }

        public async Task<ConversationDTO?> GetConversationAsync(int id)
        {
            try
            {
                using var request = await CreateRequestAsync(HttpMethod.Get, $"api/chat/conversations/{id}");
                using var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode) return null;
                return await response.Content.ReadFromJsonAsync<ConversationDTO>();
            }
            catch (HttpRequestException) { return null; }
        }

        public async Task<(ConversationDTO? Conversation, string? Error)> StartConversationAsync(int recipientId, int? productId)
        {
            try
            {
                using var request = await CreateRequestAsync(HttpMethod.Post, "api/chat/conversations");
                request.Content = JsonContent.Create(new StartConversationRequest
                {
                    RecipientId = recipientId,
                    ProductId = productId
                });

                using var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                    return (null, await ApiErrors.ReadMessageAsync(response) ?? "Nie udało się otworzyć rozmowy.");

                var conversation = await response.Content.ReadFromJsonAsync<ConversationDTO>();
                return (conversation, null);
            }
            catch (HttpRequestException)
            {
                return (null, "Nie można połączyć się z serwerem API.");
            }
        }

        public async Task<List<ChatMessageDTO>?> GetMessagesAsync(int conversationId, int? beforeId = null, int take = 50)
        {
            try
            {
                var url = $"api/chat/conversations/{conversationId}/messages?take={take}";
                if (beforeId.HasValue)
                    url += $"&beforeId={beforeId.Value}";

                using var request = await CreateRequestAsync(HttpMethod.Get, url);
                using var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode) return null;
                return await response.Content.ReadFromJsonAsync<List<ChatMessageDTO>>();
            }
            catch (HttpRequestException) { return null; }
        }

        public async Task<bool> MarkAsReadAsync(int conversationId)
        {
            try
            {
                using var request = await CreateRequestAsync(HttpMethod.Post, $"api/chat/conversations/{conversationId}/read");
                using var response = await _httpClient.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch (HttpRequestException) { return false; }
        }
    }

    // Odczyt komunikatu błędu z odpowiedzi API ({ "message": "..." }).
    internal static class ApiErrors
    {
        private sealed class ErrorBody
        {
            public string? Message { get; set; }
        }

        public static async Task<string?> ReadMessageAsync(HttpResponseMessage response)
        {
            try
            {
                var body = await response.Content.ReadFromJsonAsync<ErrorBody>();
                if (!string.IsNullOrWhiteSpace(body?.Message))
                    return body.Message;
            }
            catch (System.Text.Json.JsonException) { }
            catch (NotSupportedException) { }

            return null;
        }
    }
}