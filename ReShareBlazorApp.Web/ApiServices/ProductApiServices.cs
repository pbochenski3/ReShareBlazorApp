using ReShareBlazorApp.Shared.DTOs;
using ReShareBlazorApp.Web.Models;

namespace ReShareBlazorApp.Web.ApiServices
{
    public class ProductApiServices
    {
        private readonly HttpClient _httpClient;
        public ProductApiServices(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }
        public async Task<string> AddProductAsync(ProductModel productModel)
        {
            var product = new ProductDTO
            {
                Name = productModel.Name,
                Price = productModel.Price,
                AddedAt = productModel.AddedAt,
                ExpiresAt = productModel.ExpiresAt,
                Category = productModel.Category,
            };
            var response = await _httpClient.PostAsJsonAsync(
                "api/product/add-product",
                product
                );
            if (!response.IsSuccessStatusCode)
                return "Nie udało się dodać produkut!";
            return "Produkt dodany pomyślnie!";
        }
    }
}
