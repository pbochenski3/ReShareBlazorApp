namespace ReShareBlazorApp.Api.Date
{
    public class Message
    {
        public int Id { get; set; }

        public int ConversationId { get; set; }
        public Conversation Conversation { get; set; } = null!;

        public int SenderId { get; set; }
        public User Sender { get; set; } = null!;

        public required string Content { get; set; }

        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        // Czy drugi uczestnik rozmowy (odbiorca) już przeczytał tę wiadomość.
        public bool IsReadByRecipient { get; set; }
    }
}
