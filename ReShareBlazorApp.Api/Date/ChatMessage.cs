using System.ComponentModel.DataAnnotations;

namespace ReShareBlazorApp.Api.Date;

public sealed class ChatMessage
{
    public int Id { get; set; }
    public int ConversationId { get; set; }
    public ChatConversation Conversation { get; set; } = null!;
    public int SenderId { get; set; }
    public User Sender { get; set; } = null!;
    public int? ReplyToMessageId { get; set; }
    public ChatMessage? ReplyToMessage { get; set; }
    [Required] public string CipherText { get; set; } = "";
    [Required, MaxLength(32)] public string Nonce { get; set; } = "";
    [Required, MaxLength(32)] public string AuthTag { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
