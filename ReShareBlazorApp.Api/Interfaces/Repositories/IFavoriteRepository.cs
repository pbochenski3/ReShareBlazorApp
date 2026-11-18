using ReShareBlazorApp.Api.Date;

namespace ReShareBlazorApp.Api.Interfaces.Repositories
{
    public interface IFavoriteRepository
    {
        // true = teraz jest w ulubionych, false = usunięto, null = ogłoszenie nie istnieje
        Task<bool?> ToggleAsync(int userId, int productId);
        Task<List<int>> GetIdsAsync(int userId);
        Task<List<Product>> GetProductsAsync(int userId);
    }
}
