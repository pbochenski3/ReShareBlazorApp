using ReShareBlazorApp.Api.Date;
using ReShareBlazorApp.Api.Date.Enums;

namespace ReShareBlazorApp.Api.Interfaces.Repositories
{
    public interface IProductRepository
    {
        Task<bool> AddProductAsync(Product product);
        Task<List<Product>> GetProducts(int number = 5,ProductCategory? category = null);
    }
}
