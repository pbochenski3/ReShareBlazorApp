using ReShareBlazorApp.Api.Date.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace ReShareBlazorApp.Shared.DTOs
{
    public class ProductRequest
    {
        public int Number { get; set; } = 5;
        public List<ProductCategory>? Categories { get; set; }
        public ProductSort Sort { get; set; } = ProductSort.Newest;
    }
}
