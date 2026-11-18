using System.ComponentModel.DataAnnotations;

namespace ReShareBlazorApp.Api.Date
{
    public class ProductImage
    {
        public int Id { get; set; }

        [MaxLength(255)]
        public string FileName { get; set; } = "";

        [MaxLength(100)]
        public string ContentType { get; set; } = "";

        // nazwa pliku na dysku (w katalogu z konfiguracji ImageStorage:Path)
        [MaxLength(100)]
        public string StoredFileName { get; set; } = "";

        public int SortOrder { get; set; }

        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;
    }
}
