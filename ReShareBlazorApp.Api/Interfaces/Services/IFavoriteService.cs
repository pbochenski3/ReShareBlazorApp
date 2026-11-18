using ReShareBlazorApp.Shared.DTOs;

namespace ReShareBlazorApp.Api.Interfaces.Services
{
    public interface IFavoriteService
    {
        Task<bool?> ToggleAsync(int userId, int productId);
        Task<List<int>> GetIdsAsync(int userId);
        Task<List<ProductDTO>> GetFavoritesAsync(int userId);
    }
}
