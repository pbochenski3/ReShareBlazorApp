namespace ReShareBlazorApp.Shared.DTOs
{
    public class CreateReviewRequest
    {
        public int ProductId { get; set; }
        public string AuthorName { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string Comment { get; set; } = string.Empty;
    }
}