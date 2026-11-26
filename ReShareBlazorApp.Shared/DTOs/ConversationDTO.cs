namespace ReShareBlazorApp.Shared.DTOs
{
    public class ConversationDTO
    {
        public int ConversationId { get; set; }
        public int? ProductId { get; set; }
        public string? ProductName { get; set; }
        public int OtherUserId { get; set; }
        public string OtherUserName { get; set; } = "";
        public bool IsSupport { get; set; }
        public string? LastMessage { get; set; }
        public DateTime LastMessageAt { get; set; }
        public int UnreadCount { get; set; }
    }
}
