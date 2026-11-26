using System.ComponentModel.DataAnnotations;

namespace ReShareBlazorApp.Api.Date
{
    public class ChatMessage
    {
        public int Id { get; set; }

        public int SenderId { get; set; }
        public User Sender { get; set; } = null!;

        public int RecipientId { get; set; }
        public User Recipient { get; set; } = null!;

        public int? ProductId { get; set; }
        public Product? Product { get; set; }

        [Required]
        public string EncryptedContent { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsRead { get; set; } = false;

        public decimal? OfferPrice { get; set; }
    }
}
