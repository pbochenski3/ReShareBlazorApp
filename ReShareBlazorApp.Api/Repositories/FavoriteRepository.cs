using Microsoft.EntityFrameworkCore;
using ReShareBlazorApp.Api.Date;
using ReShareBlazorApp.Api.Interfaces.Repositories;

namespace ReShareBlazorApp.Api.Repositories
{
    public class FavoriteRepository : IFavoriteRepository
    {
        private readonly ReshareDbContext _context;
        private readonly ILogger<FavoriteRepository> _logger;
        public FavoriteRepository(ReshareDbContext context, ILogger<FavoriteRepository> logger)
        {
            _context = context;
            _logger = logger;
        }
        public async Task<bool?> ToggleAsync(int userId, int productId)
        {
            {
                // (błędy bazy celowo NIE są tu łapane - kontroler pokaże je w komunikacie)
                // sprawdzamy czy takie ogłoszenie w ogóle istnieje
                var productExists = await _context.Products.AnyAsync(p => p.Id == productId);
                if (!productExists)
                    return null;

                var existing = await _context.Favorites
                    .FirstOrDefaultAsync(f => f.UserId == userId && f.ProductId == productId);
                if (existing != null)
                {
                    _context.Favorites.Remove(existing); // było w ulubionych -> usuwamy
                    await _context.SaveChangesAsync();
                    return false;
                }
                _context.Favorites.Add(new Favorite
                {
                    UserId = userId,
                    ProductId = productId,
                    AddedAt = DateTime.Now
                });                                      // nie było -> dodajemy
                await _context.SaveChangesAsync();
                return true;
            }
        }
        public async Task<List<int>> GetIdsAsync(int userId)
        {
            try
            {
                return await _context.Favorites
                    .Where(f => f.UserId == userId)
                    .Select(f => f.ProductId)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"[FavoriteRepository] Error occured in GetIdsAsync : {ex}");
                return new List<int>();
            }
        }
        public async Task<List<Product>> GetProductsAsync(int userId)
        {
            try
            {
                return await _context.Favorites
                    .Where(f => f.UserId == userId)
                    .OrderByDescending(f => f.AddedAt)   // ostatnio dodane na górze
                    .Select(f => f.Product)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"[FavoriteRepository] Error occured in GetProductsAsync : {ex}");
                return new List<Product>();
            }
        }
    }
}
