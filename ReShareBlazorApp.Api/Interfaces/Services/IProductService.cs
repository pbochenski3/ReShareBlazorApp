using ReShareBlazorApp.Shared.DTOs;

namespace ReShareBlazorApp.Api.Interfaces.Services
{
    public interface IProductService
    {
        Task<bool> AddProductAsync(ProductDTO product);
    }
}
