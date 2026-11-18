using ReShareBlazorApp.Api.Date.Enums;
using System.ComponentModel.DataAnnotations;

namespace ReShareBlazorApp.Web.Models
{
    public class ProductModel
    {
        [Required(ErrorMessage = "Podaj nazwę produktu.")]
        [MinLength(3, ErrorMessage = "Nazwa produktu musi mieć minimum 3 znaki.")]
        [MaxLength(50, ErrorMessage = "Nazwa produktu nie może mieć więcej niż 50 znaków.")]
        public string Name { get; set; } = "";

        [Range(0.01, 100000, ErrorMessage = "Cena musi być większa od 0.")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Podaj opis produktu.")]
        [MaxLength(1000, ErrorMessage = "Opis nie może mieć więcej niż 1000 znaków.")]
        public string Description { get; set; } = "";

        [Required(ErrorMessage = "Podaj datę ważności produktu.")]
        public DateTime ExpiresAt { get; set; }

        [Required(ErrorMessage = "Wybierz kategorię produktu.")]
        public ProductCategory Category { get; set; }

        public int UserId { get; set; }
    }
}