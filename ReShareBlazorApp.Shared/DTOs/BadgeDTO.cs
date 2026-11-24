using System;
using System.Collections.Generic;
using System.Text;

namespace ReShareBlazorApp.Shared.DTOs
{
    public class BadgeDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Icon { get; set; } = "";

        public bool IsEarned { get; set; }
    }
}
