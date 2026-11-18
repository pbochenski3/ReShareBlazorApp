using ReShareBlazorApp.Api.Date.Enums;
using ReShareBlazorApp.Shared.DTOs;

namespace ReShareBlazorApp.Api.Interfaces.Services
{
    public interface IProductService
    {
        Task<bool> AddProductAsync(ProductDTO product);
        Task<bool> UpdateProductAsync(int productId, int userId, ProductUpdateDTO product);
        Task<bool> DeleteProductAsync(int productId, int userId);
        Task<List<ProductDTO>> GetProductsAsync(int number = 5,ProductCategory? category = null);
        Task<List<LocationDTO>> GetLocationsAsync(string query);
        Task<ProductDTO?> GetProductById(int id);
        string? ValidateImages(IReadOnlyList<ProductImageDTO>? images);
        Task<ProductImageDTO?> GetImageAsync(int imageId);
    }
}
