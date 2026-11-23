using ReShareBlazorApp.Shared.Enums;

namespace ReShareBlazorApp.Shared.DTOs
{
    public class NotificationDTO
    {
        public int Id { get; set; }
        public int? ProductId { get; set; }
        public NotificationType Type { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
    }
}
