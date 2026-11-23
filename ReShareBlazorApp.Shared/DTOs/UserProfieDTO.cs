namespace ReShareBlazorApp.Shared.DTOs
{
    public class UserProfileDTO
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public int UserId { get; set; }
        // Wypełniany tylko w api/user/me (własny profil).
        public string? Email { get; set; }
        public string? City { get; set; }
        public string? Bio { get; set; }
        // Ścieżka względna do API, np. "/avatars/5-abc.jpg"
        public string? AvatarUrl { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
