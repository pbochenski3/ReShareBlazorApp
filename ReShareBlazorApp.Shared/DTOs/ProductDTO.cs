using ReShareBlazorApp.Api.Date.Enums;
using ReShareBlazorApp.Shared.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ReShareBlazorApp.Shared.DTOs
{
    public class ProductDTO
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";

        public string City { get; set; } = "";
        public decimal Price { get; set; }

        public DateTime AddedAt { get; set; } = DateTime.UtcNow;

        public DateTime ExpiresAt { get; set; }
        public ProductType Type { get; set; }
        public ProductCategory Category { get; set; }
        public string AddedBy { get; set; } = "";
        public int UserId { get; set; }

        /// <summary>Zdjęcia przesyłane przy dodawaniu ogłoszenia (tylko żądanie).</summary>
        public List<ProductImageDTO> Images { get; set; } = new();

        /// <summary>Identyfikatory zdjęć zapisanych w bazie (tylko odpowiedź), pierwsze = główne.</summary>
        public List<int> ImageIds { get; set; } = new();
    }
}
