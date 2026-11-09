using ReShareBlazorApp.Api.Date;

namespace ReShareBlazorApp.Api.Interfaces.Repositories
{
    public interface IProductRepository
    {
        Task<bool> AddProductAsync(Product product);
    }
}
