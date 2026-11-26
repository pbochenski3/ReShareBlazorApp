namespace ReShareBlazorApp.Shared.DTOs
{
    public class ChatMessageDTO
    {
        public int Id { get; set; }
        public int SenderId { get; set; }
        public string SenderName { get; set; } = string.Empty;
        public string? SenderAvatar { get; set; }
        public int RecipientId { get; set; }
        public string RecipientName { get; set; } = string.Empty;
        public string? RecipientAvatar { get; set; }
        public int? ProductId { get; set; }
        public string? ProductName { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
        public decimal? OfferPrice { get; set; }
        public bool IsFromCurrentUser { get; set; }
    }
}
