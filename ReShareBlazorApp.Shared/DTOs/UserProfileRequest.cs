using System.ComponentModel.DataAnnotations;

namespace ReShareBlazorApp.Shared.DTOs
{
    public class UpdateProfileRequest
    {
        [MaxLength(60)]
        public string? City { get; set; }
        [MaxLength(300)]
        public string? Bio { get; set; }
    }
}
