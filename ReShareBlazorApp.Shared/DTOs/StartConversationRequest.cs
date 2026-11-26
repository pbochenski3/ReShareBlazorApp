using System.ComponentModel.DataAnnotations;

namespace ReShareBlazorApp.Shared.DTOs
{
    public class StartConversationRequest
    {
        public int RecipientId { get; set; }
        public int ProductId { get; set; }

        [Required(ErrorMessage = "Wiadomość jest wymagana.")]
        [StringLength(2000, MinimumLength = 1, ErrorMessage = "Wiadomość musi mieć od 1 do 2000 znaków.")]
        public string Content { get; set; } = "";
    }
}
