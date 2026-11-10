using ReShareBlazorApp.Api.Date.Enums;
using ReShareBlazorApp.Shared.DTOs;

namespace ReShareBlazorApp.Api.Interfaces.Services
{
    public interface IProductService
    {
        Task<bool> AddProductAsync(ProductDTO product);
        Task<List<ProductDTO>> GetProductsAsync(int number = 5,ProductCategory? category = null);
    }
}
