namespace ReShareBlazorApp.Shared.DTOs
{
    public class AdminReviewDTO
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = "";
        public string AuthorName { get; set; } = "";
        public int Rating { get; set; }
        public string Comment { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public bool IsHidden { get; set; }
    }
}
