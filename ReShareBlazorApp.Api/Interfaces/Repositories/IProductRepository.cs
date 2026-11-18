using ReShareBlazorApp.Api.Date;
using ReShareBlazorApp.Api.Date.Enums;

namespace ReShareBlazorApp.Api.Interfaces.Repositories
{
    public interface IProductRepository
    {
        Task<bool> AddProductAsync(Product product);
        Task<Product?> GetProductById(int id);
        Task<List<Product>> GetProductsByUser(int userId); // ogłoszenia jednego użytkownika
        Task<List<Product>> GetProducts(int number = 5, List<ProductCategory>? categories = null, ProductSort sort = ProductSort.Newest);
    }
}
