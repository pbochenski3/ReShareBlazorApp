using ReShareBlazorApp.Shared.Enums;
using System.ComponentModel.DataAnnotations;

namespace ReShareBlazorApp.Shared.DTOs
{
    public class ReportDTO
    {
        public int ReportId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = "";
        public string ProductAddedBy { get; set; } = "";

        public int UserId { get; set; }
        public string ReportedBy { get; set; } = "";

        public ReportCategory Category { get; set; }

        [Required]
        [StringLength(1000, MinimumLength = 10)]
        public string Description { get; set; } = "";

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
