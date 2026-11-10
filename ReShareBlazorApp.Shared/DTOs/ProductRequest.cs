using ReShareBlazorApp.Api.Date.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace ReShareBlazorApp.Shared.DTOs
{
    public class ProductRequest
    {
        public int Number { get; set; }
        public ProductCategory? Category { get; set; }
    }
}
