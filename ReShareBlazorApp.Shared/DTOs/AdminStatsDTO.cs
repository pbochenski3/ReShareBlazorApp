namespace ReShareBlazorApp.Shared.DTOs
{
    public class AdminStatsDTO
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int TotalProducts { get; set; }
        public int ProductsLast7Days { get; set; }
    }
}
