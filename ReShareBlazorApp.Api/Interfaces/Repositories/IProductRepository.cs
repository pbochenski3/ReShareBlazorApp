using ReShareBlazorApp.Api.Date;
using ReShareBlazorApp.Api.Date.Enums;
using ReShareBlazorApp.Shared.DTOs;

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
            string? sort = null);

        Task<List<LocationDTO>> GetLocationsAsync(string query);

        Task<Product?> GetProductById(int id);
    }
}