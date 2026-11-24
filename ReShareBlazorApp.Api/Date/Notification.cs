using System.ComponentModel.DataAnnotations;
using ReShareBlazorApp.Shared.Enums;

namespace ReShareBlazorApp.Api.Date
{
    public class Notification
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        public int? ProductId { get; set; }
        [MaxLength(256)]
        public string? ActorName { get; set; }
        public Product? Product { get; set; }
        public NotificationType Type { get; set; }
        [MaxLength(200)]
        public required string Title { get; set; }
        [MaxLength(1000)]
        public required string Message { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsRead { get; set; }
    }
}
