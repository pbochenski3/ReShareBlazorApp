using System.ComponentModel.DataAnnotations;

namespace ReShareBlazorApp.Web.Models
{
    public class EditProfileModel
    {
        // Tylko do wyświetlenia.
        public string Username { get; set; } = "";

        [MaxLength(60, ErrorMessage = "Nazwa miejscowości może mieć maksymalnie 60 znaków.")]
        public string? City { get; set; }

        [MaxLength(300, ErrorMessage = "Opis może mieć maksymalnie 300 znaków.")]
        public string? Bio { get; set; }
    }
}
