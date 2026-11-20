using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ReShareBlazorApp.Api.Date
{
    public class User : IdentityUser<int>
    {
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public bool IsActive { get; set; } = true;
        [MaxLength(60)]
        public string? City { get; set; }
        [MaxLength(300)]
        public string? Bio { get; set; }
        // Tylko nazwa pliku w folderze Avatars (np. "5-abc123.jpg").
        [MaxLength(200)]
        public string? AvatarPath { get; set; }

        // Ostrzeżenie dla użytkownika (przechowywane w istniejącej kolumnie PhoneNumber w bazie, aby nie wymagać migracji)
        [NotMapped]
        public string? WarningMessage
        {
            get => PhoneNumber;
            set => PhoneNumber = value;
        }
    }
}
