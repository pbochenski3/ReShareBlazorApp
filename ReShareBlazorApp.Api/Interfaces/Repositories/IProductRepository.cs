using ReShareBlazorApp.Api.Date;
using ReShareBlazorApp.Api.Date.Enums;
using ReShareBlazorApp.Shared.DTOs;
using ReShareBlazorApp.Shared.Enums;

namespace ReShareBlazorApp.Api.Interfaces.Repositories
{
    public interface IProductRepository
    {
        Task<bool> AddProductAsync(Product product);

        Task<List<Product>> GetProducts(
            int number = 5,
            ProductCategory? category = null,
            string? search = null,
            string? location = null,
            decimal? minPrice = null,
            decimal? maxPrice = null,
            string? sort = null,
            int? userId = null,
            ProductType? type = null);

        Task<bool> UpdateProductAsync(int productId, int userId, ProductUpdateDTO product);
        Task<bool> DeleteProductAsync(int productId, int userId);
        Task<int> CountUserProductsAsync(int userId);
        Task<bool> MarkAsSoldAsync(int productId, int userId);
        Task<Dictionary<ProductCategory, int>> GetCompletedCountsByCategoryAsync();
        Task<List<LocationDTO>> GetLocationsAsync(string query);

        Task<Product?> GetProductById(int id);
        Task<Dictionary<int, List<int>>> GetImageIdsAsync(IEnumerable<int> productIds);
        Task<ProductImage?> GetImageAsync(int imageId);

        // Ulubione. ToggleFavoriteAsync: true = dodano, false = usunięto, null = ogłoszenie nie istnieje
        Task<bool?> ToggleFavoriteAsync(int userId, int productId);
        Task<List<int>> GetFavoriteProductIdsAsync(int userId);
        Task<List<Product>> GetFavoriteProductsAsync(int userId);
    }
}