using System.ComponentModel.DataAnnotations;

namespace ReShareBlazorApp.Api.Date
{
    public class ActivityLog
    {
        public int Id { get; set; }
        // Bez klucza obcego: log zostaje, nawet gdy konto zostanie usunięte.
        public int? UserId { get; set; }
        [MaxLength(256)]
        public string UserName { get; set; } = string.Empty;
        [MaxLength(50)]
        public string Action { get; set; } = string.Empty;
        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;
        [MaxLength(50)]
        public string? TargetType { get; set; }
        public int? TargetId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
