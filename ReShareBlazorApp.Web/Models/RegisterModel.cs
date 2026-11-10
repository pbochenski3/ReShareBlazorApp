using System.ComponentModel.DataAnnotations;

namespace ReShareBlazorApp.Web.Models
{
    public class RegisterModel
    {
        [Required(ErrorMessage = "Podaj nazwe użytkownika.")]
        [MinLength(8, ErrorMessage = "Nazwa użytkownika musi mięc minimum 8 znaków.")]
        [MaxLength(24, ErrorMessage = "Nazwa użytkownika nie może mieć wiecej niż 24 znaki.")]
        public string Username { get; set; } = "";
        [Required(ErrorMessage = "Podaj adres e-mail.")]
        [EmailAddress(ErrorMessage = "Nieprawidłowy adres e-mail.")]
        public string Email { get; set; } = "";
        [Required(ErrorMessage = "Podaj hasło!")]
        [MinLength(8, ErrorMessage = "Hasło musi mięc minimum 8 znaków.")]
        [RegularExpression(
        @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$",
        ErrorMessage = "Hasło musi zawierać małą literę, dużą literę i cyfrę."
        )]
        public string Password { get; set; } = "";
        [Required(ErrorMessage = "Potwierdź hasło.")]
        [Compare(nameof(Password), ErrorMessage = "Hasła nie są identyczne.")]
        public string ConfirmPassword { get; set; } = "";
        [Range(typeof(bool), "true", "true", ErrorMessage = "Musisz zaakceptować regulamin.")]
        public bool AcceptTerms { get; set; }
    }
}
