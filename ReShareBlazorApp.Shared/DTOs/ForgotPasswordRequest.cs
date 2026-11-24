using System.ComponentModel.DataAnnotations;

namespace ReShareBlazorApp.Shared.DTOs
{
    public class ForgotPasswordRequest
    {
        [Required(ErrorMessage = "Podaj adres e-mail.")]
        [EmailAddress(ErrorMessage = "Podaj poprawny adres e-mail.")]
        public string Email { get; set; } = string.Empty;
    }
}