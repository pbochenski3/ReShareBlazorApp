using ReShareBlazorApp.Api.Date.Enums;
using ReShareBlazorApp.Shared.DTOs;
using ReShareBlazorApp.Shared.Enums;

namespace ReShareBlazorApp.Api.Interfaces.Services
{
    public interface IProductService
    {
        Task<bool> AddProductAsync(ProductDTO product);

        Task<List<ProductDTO>> GetProductsAsync(
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
        Task<bool> MarkAsSoldAsync(int productId, int userId);
        Task<CompletedStatsDTO> GetCompletedStatsAsync();
        Task<List<LocationDTO>> GetLocationsAsync(string query);
        Task<ProductDTO?> GetProductById(int id);
        string? ValidateImages(IReadOnlyList<ProductImageDTO>? images);
        Task<ProductImageDTO?> GetImageAsync(int imageId);
        Task<bool?> ToggleFavoriteAsync(int userId, int productId);
        Task<List<int>> GetFavoriteIdsAsync(int userId);
        Task<List<ProductDTO>> GetFavoritesAsync(int userId);
    }
}