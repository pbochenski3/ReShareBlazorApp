using ReShareBlazorApp.Shared.DTOs;

namespace ReShareBlazorApp.Api.Interfaces.Services
{
    public interface IMessageService
    {
        Task<List<ConversationDTO>> GetConversationsAsync(int userId);

        Task<(ConversationDTO Conversation, List<MessageDTO> Messages)?> GetMessagesAsync(
            int conversationId, int userId);

        Task<ConversationDTO?> StartConversationAsync(int senderId, StartConversationRequest request);

        Task<MessageDTO?> SendMessageAsync(int senderId, int conversationId, string content);

        Task<ConversationDTO?> StartSupportConversationAsync(int adminId, int reportId, string content);
    }
}
