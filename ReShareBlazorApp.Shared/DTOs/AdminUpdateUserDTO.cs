using System.ComponentModel.DataAnnotations;

namespace ReShareBlazorApp.Shared.DTOs;

public class AdminUpdateUserDTO
{
    [Required(ErrorMessage = "Nazwa użytkownika jest wymagana.")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Nazwa użytkownika musi mieć od 3 do 50 znaków.")]
    public string UserName { get; set; } = "";

    [Required(ErrorMessage = "Adres e-mail jest wymagany.")]
    [EmailAddress(ErrorMessage = "Niepoprawny format adresu e-mail.")]
    public string Email { get; set; } = "";

    [Phone(ErrorMessage = "Niepoprawny format numeru telefonu.")]
    [MaxLength(20, ErrorMessage = "Numer telefonu może mieć maksymalnie 20 znaków.")]
    public string? PhoneNumber { get; set; }

    [MaxLength(60, ErrorMessage = "Miejscowość może mieć maksymalnie 60 znaków.")]
    public string? City { get; set; }

    [MaxLength(300, ErrorMessage = "Bio może mieć maksymalnie 300 znaków.")]
    public string? Bio { get; set; }

    public bool IsActive { get; set; } = true;
}
