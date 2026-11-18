using ReShareBlazorApp.Api.Date;
using ReShareBlazorApp.Api.Date.Enums;
using ReShareBlazorApp.Shared.DTOs;

namespace ReShareBlazorApp.Api.Interfaces.Repositories
{
    public interface IProductRepository
    {
        Task<bool> AddProductAsync(Product product);
        Task<List<Product>> GetProducts(int number = 5,ProductCategory? category = null);
        Task<List<LocationDTO>> GetLocationsAsync(string query);
        Task<Product?> GetProductById(int id);
        Task<Dictionary<int, List<int>>> GetImageIdsAsync(IEnumerable<int> productIds);
        Task<ProductImage?> GetImageAsync(int imageId);
    }
}
