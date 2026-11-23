using ReShareBlazorApp.Api.Date.Enums;
using ReShareBlazorApp.Shared.Enums;

namespace ReShareBlazorApp.Shared.DTOs
{
    public class ProductUpdateDTO
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string City { get; set; } = "";
        public decimal Price { get; set; }
        public decimal PriceMin { get; set; }
        public decimal PriceMax { get; set; }
        public DateTime ExpiresAt { get; set; }
        public ProductType Type { get; set; }
        public ProductCategory Category { get; set; }
        public ProductCondition Condition { get; set; }
    }
}
