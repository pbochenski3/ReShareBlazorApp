using ReShareBlazorApp.Shared.DTOs;

namespace ReShareBlazorApp.Api.Interfaces.Services
{
    public interface IMessageService
    {
        Task<List<ConversationSummaryDTO>> GetConversationsAsync(int currentUserId);
        Task<List<ChatMessageDTO>> GetConversationMessagesAsync(int currentUserId, int otherUserId, int? productId = null);
        Task<ChatMessageDTO?> SendMessageAsync(int currentUserId, SendMessageRequest request);
        Task<bool> MarkConversationAsReadAsync(int currentUserId, int otherUserId);
    }
}
