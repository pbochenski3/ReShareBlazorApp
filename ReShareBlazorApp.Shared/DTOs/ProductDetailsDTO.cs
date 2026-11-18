using ReShareBlazorApp.Api.Date.Enums;

namespace ReShareBlazorApp.Shared.DTOs
{
    public class ProductDetailsDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public decimal Price { get; set; }
        public DateTime AddedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public ProductCategory Category { get; set; }
        public int UserId { get; set; }
        public string SellerName { get; set; } = "";
        public string SellerEmail { get; set; } = "";
        public DateTime SellerSince { get; set; }
    }
}
