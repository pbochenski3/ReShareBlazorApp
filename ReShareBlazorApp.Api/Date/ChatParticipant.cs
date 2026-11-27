namespace ReShareBlazorApp.Api.Date;

public sealed class ChatParticipant
{
    public int ConversationId { get; set; }
    public ChatConversation Conversation { get; set; } = null!;
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastReadAt { get; set; }
}
