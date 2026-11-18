using System.ComponentModel.DataAnnotations;

namespace ReShareBlazorApp.Api.Date
{
    public class PendingRegistration
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(24)]
        public string UserName { get; set; } = "";

        [Required]
        [MaxLength(256)]
        public string Email { get; set; } = "";

        [Required]
        public string PasswordHash { get; set; } = "";

        /// <summary>
        /// 6-cyfrowy kod weryfikacyjny
        /// </summary>
        [Required]
        [MaxLength(6)]
        public string VerificationCode { get; set; } = "";

        /// <summary>
        /// Token (GUID) do potwierdzenia linkiem
        /// </summary>
        [Required]
        [MaxLength(64)]
        public string VerificationToken { get; set; } = "";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime ExpiresAt { get; set; }
    }
}
