using System.ComponentModel.DataAnnotations;

namespace ReShareBlazorApp.Shared.DTOs;

public class ContactRequest
{
    [Required(ErrorMessage = "Imię jest wymagane.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Adres e-mail jest wymagany.")]
    [EmailAddress(ErrorMessage = "Niepoprawny adres e-mail.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tytuł wiadomości jest wymagany.")]
    [StringLength(120, ErrorMessage = "Tytuł może mieć maksymalnie 120 znaków.")]
    public string Subject { get; set; } = string.Empty;

    [Required(ErrorMessage = "Treść wiadomości jest wymagana.")]
    [MinLength(5, ErrorMessage = "Wiadomość jest za krótka.")]
    public string Message { get; set; } = string.Empty;
}