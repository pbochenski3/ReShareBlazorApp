using System.ComponentModel.DataAnnotations;

namespace ReShareBlazorApp.Api.Date
{
    public static class ChatLogActions
    {
        public const string Sent = "Sent";
        public const string SupportSent = "SupportSent";
        public const string SupportReply = "SupportReply";
    }

    // Niezmienny log wysłanych wiadomości (zaszyfrowana kopia treści).
    public class ChatMessageLog
    {
        public int Id { get; set; }
        public int? MessageId { get; set; }
        public ChatMessage? Message { get; set; }
        public int ConversationId { get; set; }
        public int SenderId { get; set; }
        public int? RecipientId { get; set; }

        [MaxLength(30)]
        public string Action { get; set; } = ChatLogActions.Sent;

        public string EncryptedContent { get; set; } = string.Empty;

        [MaxLength(64)]
        public string? IpAddress { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}