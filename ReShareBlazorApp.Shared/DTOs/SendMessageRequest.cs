using System.ComponentModel.DataAnnotations;

namespace ReShareBlazorApp.Shared.DTOs
{
    public class SendMessageRequest
    {
        [Required]
        public int RecipientId { get; set; }

        public int? ProductId { get; set; }

        [Required(ErrorMessage = "Treść wiadomości jest wymagana.")]
        [StringLength(4000, MinimumLength = 1, ErrorMessage = "Wiadomość musi mieć od 1 do 4000 znaków.")]
        public string Content { get; set; } = string.Empty;

        public decimal? OfferPrice { get; set; }
    }
}
