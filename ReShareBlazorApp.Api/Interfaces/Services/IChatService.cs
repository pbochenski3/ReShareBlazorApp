using ReShareBlazorApp.Shared.DTOs;

namespace ReShareBlazorApp.Api.Interfaces.Services
{
    public class ChatException : Exception
    {
        public ChatException(string message) : base(message) { }
    }

    public interface IChatService
    {
        Task<ConversationDTO> StartConversationAsync(int currentUserId, int recipientId, int? productId);
        Task<List<ConversationDTO>> GetConversationsAsync(int userId);
        Task<List<ConversationDTO>> GetSupportConversationsAsync();
        Task<ConversationDTO?> GetConversationAsync(int userId, bool isAdmin, int conversationId);
        Task<List<ChatMessageDTO>?> GetMessagesAsync(int userId, bool isAdmin, int conversationId, int? beforeId, int take);
        Task<ChatMessageDTO> SendMessageAsync(int senderId, bool senderIsAdmin, int conversationId, string content, string? ipAddress);
        Task<ChatMessageDTO> SendSupportMessageToUserAsync(int adminId, int customerId, string content, string? ipAddress);
        Task<bool> MarkAsReadAsync(int userId, bool isAdmin, int conversationId);
    }
}