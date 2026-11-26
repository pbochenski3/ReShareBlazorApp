namespace ReShareBlazorApp.Shared.DTOs
{
    public class ConversationSummaryDTO
    {
        public int OtherUserId { get; set; }
        public string OtherUserName { get; set; } = string.Empty;
        public string? OtherUserAvatar { get; set; }
        public int? ProductId { get; set; }
        public string? ProductName { get; set; }
        public string LastMessage { get; set; } = string.Empty;
        public DateTime LastMessageDate { get; set; }
        public int UnreadCount { get; set; }
    }
}
