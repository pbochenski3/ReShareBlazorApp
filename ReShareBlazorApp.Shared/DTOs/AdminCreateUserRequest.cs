using System.ComponentModel.DataAnnotations;

namespace ReShareBlazorApp.Shared.DTOs
{
    public class AdminCreateUserRequest
    {
        [Required(ErrorMessage = "Nazwa użytkownika jest wymagana.")]
        [MinLength(3, ErrorMessage = "Nazwa użytkownika musi mieć co najmniej 3 znaki.")]
        [MaxLength(50, ErrorMessage = "Nazwa użytkownika nie może przekraczać 50 znaków.")]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Adres e-mail jest wymagany.")]
        [EmailAddress(ErrorMessage = "Nieprawidłowy format adresu e-mail.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Hasło jest wymagane.")]
        [MinLength(8, ErrorMessage = "Hasło musi mieć co najmniej 8 znaków.")]
        public string Password { get; set; } = string.Empty;

        public bool IsAdmin { get; set; } = false;
    }
}
