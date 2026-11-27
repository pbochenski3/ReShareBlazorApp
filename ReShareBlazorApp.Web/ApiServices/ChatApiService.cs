using System.Net.Http.Headers;
using System.Net.Http.Json;
using ReShareBlazorApp.Shared.DTOs;

namespace ReShareBlazorApp.Web.ApiServices;

public sealed class ChatApiService
{
    private readonly IHttpClientFactory _factory;
    public ChatApiService(IHttpClientFactory factory) => _factory = factory;

    private HttpClient Client(string token)
    {
        var client = _factory.CreateClient("API");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public async Task<List<ChatConversationDto>> GetConversationsAsync(string token) =>
        await Client(token).GetFromJsonAsync<List<ChatConversationDto>>("api/chat/conversations") ?? new();

    public async Task<List<ChatMessageDto>> GetMessagesAsync(string token, int conversationId) =>
        await Client(token).GetFromJsonAsync<List<ChatMessageDto>>($"api/chat/conversations/{conversationId}/messages") ?? new();

    public async Task<int?> CreateConversationAsync(string token, int otherUserId)
    {
        var response = await Client(token).PostAsJsonAsync("api/chat/conversations",
            new CreateConversationRequest { OtherUserId = otherUserId });
        if (!response.IsSuccessStatusCode) return null;
        var result = await response.Content.ReadFromJsonAsync<ConversationIdResponse>();
        return result?.ConversationId;
    }

    public async Task<ChatMessageDto?> SendAsync(string token, int conversationId, string text, int? replyToId)
    {
        var response = await Client(token).PostAsJsonAsync($"api/chat/conversations/{conversationId}/messages",
            new SendChatMessageRequest { Text = text, ReplyToMessageId = replyToId });
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<ChatMessageDto>();
    }

    public async Task MarkReadAsync(string token, int conversationId)
    {
        using var response = await Client(token).PostAsync($"api/chat/conversations/{conversationId}/read", null);
    }

    public async Task<int?> OpenAdminReportAsync(string token, int reportId)
    {
        var response = await Client(token).PostAsync($"api/chat/admin/reports/{reportId}", null);
        if (!response.IsSuccessStatusCode) return null;
        return (await response.Content.ReadFromJsonAsync<ConversationIdResponse>())?.ConversationId;
    }

    public async Task<int?> OpenAdminContactAsync(string token, int contactId)
    {
        var response = await Client(token).PostAsync($"api/chat/admin/contact/{contactId}", null);
        if (!response.IsSuccessStatusCode) return null;
        return (await response.Content.ReadFromJsonAsync<ConversationIdResponse>())?.ConversationId;
    }

    private sealed class ConversationIdResponse { public int ConversationId { get; set; } }
}
