using System;
using System.Collections.Generic;
using System.Text;

namespace ReShareBlazorApp.Shared.DTOs
{
    public class NotificationDTO
    {
        public string Title { get; set; }
        public string Message { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
