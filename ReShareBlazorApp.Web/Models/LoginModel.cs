using System.ComponentModel.DataAnnotations;

namespace ReShareBlazorApp.Web.Models
{
    public class LoginModel
    {
        [Required(ErrorMessage = "Podaj adres e-mail.")]
        [EmailAddress(ErrorMessage = "Nieprawidłowy adres e-mail.")]
        public string Email { get; set; } = "";
        [Required(ErrorMessage = "Podaj hasło!")]
        public string Password { get; set; } = "";
    }
}
