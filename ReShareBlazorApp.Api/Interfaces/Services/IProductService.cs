using ReShareBlazorApp.Api.Date.Enums;
using ReShareBlazorApp.Shared.DTOs;

namespace ReShareBlazorApp.Api.Interfaces.Services
{
    public interface IProductService
    {
        Task<bool> AddProductAsync(ProductDTO product);
        Task<ProductDetailsDTO?> GetProductByIdAsync(int id);
        Task<List<ProductDTO>> GetUserProductsAsync(int userId);
        Task<List<ProductDTO>> GetProductsAsync(int number = 5, List<ProductCategory>? categories = null, ProductSort sort = ProductSort.Newest);
    }
}
