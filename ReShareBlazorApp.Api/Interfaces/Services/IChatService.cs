using System.Security.Claims;
using ReShareBlazorApp.Shared.DTOs;

namespace ReShareBlazorApp.Api.Interfaces.Services;

public interface IChatService
{
    Task<List<ChatConversationDTO>> GetConversationsAsync(int currentUserId);
    Task<(ChatConversationDTO? Conversation, string? Error)> GetOrCreateConversationAsync(int currentUserId, int recipientId, int? productId);
    Task<(List<ChatMessageDTO>? Messages, string? Error)> GetMessagesAsync(int currentUserId, int conversationId);
    Task<(ChatMessageDTO? Message, string? Error)> SendMessageAsync(int senderId, SendChatMessageRequest request, bool isAdminMessage = false);
    Task<int> MarkConversationAsReadAsync(int currentUserId, int conversationId);
    Task<int> GetUnreadCountAsync(int currentUserId);
    Task<List<ChatUserLookupDTO>> SearchUsersAsync(int currentUserId, string? query);

    // Panel admina: odpowiedzi na wiadomości kontaktowe i zgłoszenia
    Task<List<ChatMessageDTO>> GetContactThreadAsync(int contactMessageId);
    Task<AdminReplyResultDTO> SendAdminContactReplyAsync(ClaimsPrincipal adminPrincipal, int adminUserId, int contactMessageId, AdminContactReplyRequest request);
    Task<AdminReportThreadsDTO?> GetReportThreadsAsync(int reportId);
    Task<AdminReplyResultDTO> SendAdminReportReplyAsync(ClaimsPrincipal adminPrincipal, int adminUserId, int reportId, AdminReportReplyRequest request);

    // Logi wiadomości (szyfrowane)
    Task<EncryptedMessageLogPageDTO> GetMessageLogsAsync(string? search, string? eventType, int page, int pageSize);
    Task<DecryptedMessageLogDTO?> DecryptMessageLogAsync(ClaimsPrincipal adminPrincipal, int logId);
}
