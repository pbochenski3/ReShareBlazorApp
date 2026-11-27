using System.ComponentModel.DataAnnotations;

namespace ReShareBlazorApp.Api.Date
{
    public class AdminReply
    {
        public int Id { get; set; }
        public int? ContactMessageId { get; set; }
        public int? ReportId { get; set; }
        public int AdminId { get; set; }

        [MaxLength(256)]
        public string AdminName { get; set; } = string.Empty;

        public int? RecipientUserId { get; set; }

        [MaxLength(256)]
        public string? RecipientEmail { get; set; }

        [MaxLength(20)]
        public string Channel { get; set; } = "Email";

        public string EncryptedBody { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}