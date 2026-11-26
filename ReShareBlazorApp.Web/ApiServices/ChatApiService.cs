using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components.Authorization;
using ReShareBlazorApp.Shared.DTOs;

namespace ReShareBlazorApp.Web.ApiServices;

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

    public async Task<List<ChatConversationDTO>> GetConversationsAsync()
    {
        using var request = await CreateRequestAsync(HttpMethod.Get, "api/chat/conversations");
        if (request is null) return new();

        using var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode) return new();

        return await response.Content.ReadFromJsonAsync<List<ChatConversationDTO>>() ?? new();
    }

    public async Task<(ChatConversationDTO? Conversation, string? Error)> StartOrGetConversationAsync(
        int recipientId, int? productId = null)
    {
        using var request = await CreateRequestAsync(HttpMethod.Post, "api/chat/conversations/start");
        if (request is null) return (null, "Brak autoryzacji.");

        request.Content = JsonContent.Create(new StartConversationRequest
        {
            RecipientId = recipientId,
            ProductId = productId
        });

        using var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var err = await ReadErrorAsync(response);
            return (null, err ?? "Nie udało się rozpocząć rozmowy.");
        }

        var dto = await response.Content.ReadFromJsonAsync<ChatConversationDTO>();
        return (dto, null);
    }

    public async Task<List<ChatMessageDTO>> GetMessagesAsync(int conversationId)
    {
        using var request = await CreateRequestAsync(HttpMethod.Get, $"api/chat/conversations/{conversationId}/messages");
        if (request is null) return new();

        using var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode) return new();

        return await response.Content.ReadFromJsonAsync<List<ChatMessageDTO>>() ?? new();
    }

    public async Task<(ChatMessageDTO? Message, string? Error)> SendMessageAsync(SendChatMessageRequest model)
    {
        using var request = await CreateRequestAsync(HttpMethod.Post, "api/chat/messages");
        if (request is null) return (null, "Brak autoryzacji.");

        request.Content = JsonContent.Create(model);
        using var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var err = await ReadErrorAsync(response);
            return (null, err ?? "Nie udało się wysłać wiadomości.");
        }

        var dto = await response.Content.ReadFromJsonAsync<ChatMessageDTO>();
        return (dto, null);
    }

    public async Task<int> GetUnreadCountAsync()
    {
        using var request = await CreateRequestAsync(HttpMethod.Get, "api/chat/unread-count");
        if (request is null) return 0;

        using var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode) return 0;

        return await response.Content.ReadFromJsonAsync<int>();
    }

    public async Task MarkConversationReadAsync(int conversationId)
    {
        using var request = await CreateRequestAsync(HttpMethod.Post, $"api/chat/conversations/{conversationId}/read");
        if (request is null) return;
        await _httpClient.SendAsync(request);
    }

    public async Task<List<ChatUserLookupDTO>> SearchUsersAsync(string? query)
    {
        var url = string.IsNullOrWhiteSpace(query)
            ? "api/chat/users/search"
            : $"api/chat/users/search?q={Uri.EscapeDataString(query.Trim())}";

        using var request = await CreateRequestAsync(HttpMethod.Get, url);
        if (request is null) return new();

        using var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode) return new();

        return await response.Content.ReadFromJsonAsync<List<ChatUserLookupDTO>>() ?? new();
    }

    private static async Task<string?> ReadErrorAsync(HttpResponseMessage response)
    {
        try
        {
            var body = await response.Content.ReadFromJsonAsync<ErrorBody>();
            return body?.Message;
        }
        catch
        {
            return null;
        }
    }

    private sealed class ErrorBody
    {
        public string? Message { get; set; }
    }
}
