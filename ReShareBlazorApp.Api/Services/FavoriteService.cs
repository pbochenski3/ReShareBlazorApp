using ReShareBlazorApp.Api.Interfaces.Repositories;
using ReShareBlazorApp.Api.Interfaces.Services;
using ReShareBlazorApp.Shared.DTOs;

namespace ReShareBlazorApp.Api.Services
{
    public class FavoriteService : IFavoriteService
    {
        private readonly IFavoriteRepository _favoriteRepository;
        public FavoriteService(IFavoriteRepository favoriteRepository)
        {
            _favoriteRepository = favoriteRepository;
        }
        public Task<bool?> ToggleAsync(int userId, int productId)
            => _favoriteRepository.ToggleAsync(userId, productId);

        public Task<List<int>> GetIdsAsync(int userId)
            => _favoriteRepository.GetIdsAsync(userId);

        public async Task<List<ProductDTO>> GetFavoritesAsync(int userId)
        {
            var products = await _favoriteRepository.GetProductsAsync(userId);
            return products.Select(p => new ProductDTO
            {
                Id = p.Id,
                Name = p.Name,
                Price = p.Price,
                Description = p.Description,
                AddedAt = p.AddedAt,
                ExpiresAt = p.ExpiresAt,
                Category = p.Category,
                UserId = p.UserId,
            }).ToList();
        }
    }
}
