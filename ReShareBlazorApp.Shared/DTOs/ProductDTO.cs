using ReShareBlazorApp.Api.Date.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ReShareBlazorApp.Shared.DTOs
{
    public class ProductDTO
    {
        public string Name { get; set; } = "";

        public decimal Price { get; set; }

        public DateTime AddedAt { get; set; } = DateTime.Now;

        public DateTime ExpiresAt { get; set; }

        public ProductCategory Category { get; set; }

        public int UserId { get; set; }
    }
}
