namespace ReShareBlazorApp.Shared.DTOs
{
    public class MessageDTO
    {
        public int Id { get; set; }
        public int ConversationId { get; set; }
        public int SenderId { get; set; }
        public string SenderName { get; set; } = "";
        public string Content { get; set; } = "";
        public DateTime SentAt { get; set; }
        public bool IsMine { get; set; }
    }
}
