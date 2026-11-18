using System.ComponentModel.DataAnnotations;

namespace ReShareBlazorApp.Shared.DTOs;

public class ContactRequest
{
    [Required(ErrorMessage = "Podaj swoje imię")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Podaj adres e-mail")]
    [EmailAddress(ErrorMessage = "Nieprawidłowy format adresu e-mail")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Wpisz treść wiadomości")]
    [MinLength(10, ErrorMessage = "Wiadomość musi mieć minimum 10 znaków")]
    public string Message { get; set; } = string.Empty;
}