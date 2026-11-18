using ReShareBlazorApp.Shared.Enums;
using System.ComponentModel.DataAnnotations;

namespace ReShareBlazorApp.Web.Models
{
    public class ReportModel
    {
        [Required(ErrorMessage = "Wybierz kategorię zgłoszenia.")]
        public ReportCategory? Category { get; set; }

        [Required(ErrorMessage = "Opisz powód zgłoszenia.")]
        [MinLength(10, ErrorMessage = "Opis musi mieć minimum 10 znaków.")]
        [MaxLength(1000, ErrorMessage = "Opis nie może mieć więcej niż 1000 znaków.")]
        public string Description { get; set; } = "";
    }
}
