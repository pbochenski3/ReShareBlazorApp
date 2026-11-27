namespace ReShareBlazorApp.Api.Date
{
    public class ChatMessage
    {
        public int Id { get; set; }
        public int ConversationId { get; set; }
        public Conversation Conversation { get; set; } = null!;
        public int SenderId { get; set; }

        // Treść zaszyfrowana (AES-256-GCM, Base64).
        public string EncryptedContent { get; set; } = string.Empty;

        public DateTime SentAt { get; set; } = DateTime.UtcNow;
        public bool IsRead { get; set; }
        public DateTime? ReadAt { get; set; }
    }
}