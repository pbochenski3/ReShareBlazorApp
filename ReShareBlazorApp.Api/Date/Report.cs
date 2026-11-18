using ReShareBlazorApp.Shared.Enums;

namespace ReShareBlazorApp.Api.Date
{
    public class Report
    {
        public int Id { get; set; }

        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;

        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public ReportCategory Category { get; set; }
        public string Description { get; set; } = "";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
