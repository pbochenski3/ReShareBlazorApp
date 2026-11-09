
using ReShareBlazorApp.Api.Date.Enums;

namespace ReShareBlazorApp.Api.Date
{
    public class Product
    {
        public required string Name { get; set; }
        public decimal Price { get; set; }
        public DateTime AddedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public ProductCategory Category { get; set; }
        public int UserId { get; set; }
        public User user { get; set; }
    }
}
