using System.ComponentModel.DataAnnotations;

namespace ReShareBlazorApp.Shared.DTOs
{
    public class ReplyReportRequest
    {
        [Required(ErrorMessage = "Odpowiedź nie może być pusta.")]
        [StringLength(2000, MinimumLength = 2, ErrorMessage = "Odpowiedź musi mieć od 2 do 2000 znaków.")]
        public string Response { get; set; } = string.Empty;
    }
}
