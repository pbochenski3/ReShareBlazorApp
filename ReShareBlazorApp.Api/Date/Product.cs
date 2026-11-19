
using ReShareBlazorApp.Api.Date.Enums;
using ReShareBlazorApp.Shared.Enums;

namespace ReShareBlazorApp.Api.Date
{
    public class Product
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string Description { get; set; }

        public decimal Price { get; set; }
        public DateTime AddedAt { get; set; } = DateTime.Now;
        public DateTime ExpiresAt { get; set; }
        public ProductType Type { get; set; }
        public ProductCategory Category { get; set; }
        public required string City { get; set; }
        public int UserId { get; set; }
        public User user { get; set; }
        public bool isDeleted { get; set; } = false;
        public List<ProductImage> Images { get; set; } = new();
    }
}
