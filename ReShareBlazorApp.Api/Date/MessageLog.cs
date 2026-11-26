using System.ComponentModel.DataAnnotations;

namespace ReShareBlazorApp.Api.Date
{
    public class MessageLog
    {
        public int Id { get; set; }

        public int MessageId { get; set; }

        public int SenderId { get; set; }

        [MaxLength(256)]
        public string SenderName { get; set; } = string.Empty;

        public int RecipientId { get; set; }

        [MaxLength(256)]
        public string RecipientName { get; set; } = string.Empty;

        public int? ProductId { get; set; }

        [MaxLength(300)]
        public string? ProductName { get; set; }

        [Required]
        public string EncryptedContent { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
