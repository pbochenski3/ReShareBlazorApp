using Microsoft.EntityFrameworkCore;
using ReShareBlazorApp.Api.Date;
using ReShareBlazorApp.Api.Date.Enums;
using ReShareBlazorApp.Api.Interfaces.Repositories;
using ReShareBlazorApp.Shared.DTOs;
using ReShareBlazorApp.Shared.Enums;

namespace ReShareBlazorApp.Api.Repositories
{

    public class ProductRepository : IProductRepository
    {
        private readonly ReshareDbContext _context;
        private readonly ILogger<ProductRepository> _logger;
        public ProductRepository(ReshareDbContext context, ILogger<ProductRepository> logger)
        {
            _context = context;
            _logger = logger;
        }
        public async Task<bool> AddProductAsync(Product product)
        {
            try
            {
                await _context.AddAsync(product);
                await _context.SaveChangesAsync();

                var productsCount = await _context.Products
                    .CountAsync(p => p.UserId == product.UserId);

                if (productsCount >= 1)
                    await AwardBadgeIfNeededAsync(product.UserId, "Pierwsze ogłoszenie");

                if (productsCount >= 5)
                    await AwardBadgeIfNeededAsync(product.UserId, "Aktywny sprzedawca");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ProductRepository] Error occurred in AddProductAsync");
                return false;
            }
        }
        public async Task<bool> UpdateProductAsync(int productId, int userId, ProductUpdateDTO product)
        {
            try
            {
                var existingProduct = await _context.Products
                    .FirstOrDefaultAsync(p => p.Id == productId && p.UserId == userId);

                if (existingProduct == null)
                    return false;

                existingProduct.Name = product.Name;
                existingProduct.Description = product.Description;
                existingProduct.Price = product.PriceMin;
                existingProduct.PriceMin = product.PriceMin;
                existingProduct.PriceMax = product.PriceMax;
                existingProduct.ExpiresAt = product.ExpiresAt;
                existingProduct.Type = product.Type;
                existingProduct.Category = product.Category;
                existingProduct.Condition = product.Condition;
                existingProduct.City = product.City;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ProductRepository] Error occurred in UpdateProductAsync");
                return false;
            }
        }
          
        public async Task<bool> DeleteProductAsync(int productId, int userId)
        {
            try
            {
                var rows = await _context.Products.Where(p => p.Id == productId && p.UserId == userId)
                    .ExecuteUpdateAsync(p => p.SetProperty(p => p.isDeleted, true));
                return rows > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ProductRepository] Error occurred in DeleteProductAsync");
                return false;
            }
        }


        public async Task<bool> MarkAsSoldAsync(int productId, int userId)
        {
            try
            {
                var rows = await _context.Products
                    .Where(p =>
                        p.Id == productId &&
                        p.UserId == userId &&
                        !p.isSold)
                    .ExecuteUpdateAsync(p => p
                        .SetProperty(x => x.isDeleted, true)
                        .SetProperty(x => x.isSold, true));

                if (rows == 0)
                    return false;

                // Liczymy sprzedane produkty użytkownika.
                var soldProductsCount = await _context.Products
                    .IgnoreQueryFilters()
                    .CountAsync(p =>
                        p.UserId == userId &&
                        p.isSold);

                // Pierwsza sprzedaż.
                if (soldProductsCount >= 1)
                {
                    await AwardBadgeIfNeededAsync(
                        userId,
                        "Pierwsza sprzedaż");
                }

                // Dziesięć sprzedanych produktów.
                if (soldProductsCount >= 10)
                {
                    await AwardBadgeIfNeededAsync(
                        userId,
                        "Diamentowy sprzedawca");
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "[ProductRepository] Error occurred in MarkAsSoldAsync");

                return false;
            }
        }


        // Licznik: ogłoszenia oznaczone jako sprzedane (isDeleted = true), pogrupowane po kategorii
        public async Task<Dictionary<ProductCategory, int>> GetCompletedCountsByCategoryAsync()
        {
            try
            {
                var rows = await _context.Products
                    .AsNoTracking()
                    .IgnoreQueryFilters()
                    .Where(p => p.isDeleted)
                    .GroupBy(p => p.Category)
                    .Select(g => new { Category = g.Key, Count = g.Count() })
                    .ToListAsync();

                return rows.ToDictionary(r => r.Category, r => r.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ProductRepository] Error occurred in GetCompletedCountsByCategoryAsync");
                throw;
            }
        }

        public async Task<int> CountUserProductsAsync(int userId)
        {
            try
            {
                return await _context.Products
                    .CountAsync(p => p.UserId == userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in CountUserProductsAsync for user {UserId}", userId);
                throw;
            }
        }

        public async Task<List<Product>> GetProducts(
    int number = 5,
    ProductCategory? category = null,
    string? search = null,
    string? location = null,
    decimal? minPrice = null,
    decimal? maxPrice = null,
    string? sort = null,
    int? userId = null,
    ProductType? type = null)
        {
            try
            {
                var query = _context.Products
                    .AsNoTracking()
                    .Where(p => !p.isDeleted)
                    .AsQueryable();
                if (userId.HasValue)
                {
                    query = query.Where(p => p.UserId == userId.Value);
                }
                if (type.HasValue)
                {
                    query = query.Where(p => p.Type == type.Value);
                }
                if (!string.IsNullOrWhiteSpace(search))
                {
                    var phrase = search.Trim().ToLower();

                    query = query.Where(p =>
                        p.Name.ToLower().Contains(phrase) ||
                        p.Description.ToLower().Contains(phrase));
                }
                if (category.HasValue)
                {
                    query = query.Where(p => p.Category == category.Value);
                }
                if (!string.IsNullOrWhiteSpace(location))
                {
                    var city = location.Trim().ToLower();

                    query = query.Where(p =>
                        p.City.ToLower().Contains(city));
                }
                if (minPrice.HasValue)
                {
                    query = query.Where(p => p.PriceMax >= minPrice.Value);
                }
                if (maxPrice.HasValue)
                {
                    query = query.Where(p => p.PriceMin <= maxPrice.Value);
                }
                query = sort switch
                {
                    "cenaRosnaco" => query.OrderBy(p => p.PriceMin),

                    "cenaMalejaco" => query.OrderByDescending(p => p.PriceMax),

                    _ => query.OrderByDescending(p => p.AddedAt)
                };
                if (number > 0)
                {
                    query = query.Take(number);
                }

                return await query.ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in GetProducts");
                throw;
            }
        }
        public async Task<Product?> GetProductById(int id)
        {
            try
            {
                var product = await _context.Products
                    .Include(p => p.user)
                    .Include(p => p.Images)
                    .FirstOrDefaultAsync(p => p.Id == id);

                return product;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ProductRepository] Error occurred in GetProductById");
                throw;
            }
        }
        // Pobiera tylko identyfikatory zdjęć (bez zawartości binarnej),
        // żeby listy ogłoszeń nie ładowały obrazków do pamięci.
        public async Task<Dictionary<int, List<int>>> GetImageIdsAsync(IEnumerable<int> productIds)
        {
            var ids = productIds.ToList();
            if (ids.Count == 0)
                return new Dictionary<int, List<int>>();

            var rows = await _context.ProductImages
                .AsNoTracking()
                .Where(i => ids.Contains(i.ProductId))
                .OrderBy(i => i.SortOrder)
                .ThenBy(i => i.Id)
                .Select(i => new { i.ProductId, i.Id })
                .ToListAsync();

            return rows
                .GroupBy(r => r.ProductId)
                .ToDictionary(g => g.Key, g => g.Select(r => r.Id).ToList());
        }
        public async Task<ProductImage?> GetImageAsync(int imageId)
        {
            return await _context.ProductImages
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == imageId);
        }

        public async Task<bool?> ToggleFavoriteAsync(int userId, int productId)
        {
            try
            {
                var favorite = await _context.FavoriteProducts
                    .FirstOrDefaultAsync(f =>
                        f.UserId == userId &&
                        f.ProductId == productId);

                // Jeśli produkt jest już ulubiony, usuwamy go.
                if (favorite != null)
                {
                    _context.FavoriteProducts.Remove(favorite);
                    await _context.SaveChangesAsync();

                    return false;
                }

                // Sprawdzamy, czy produkt istnieje i nie jest usunięty.
                var productExists = await _context.Products
                    .AnyAsync(p => p.Id == productId);

                if (!productExists)
                    return null;

                // Dodajemy produkt do ulubionych.
                _context.FavoriteProducts.Add(new FavoriteProduct
                {
                    UserId = userId,
                    ProductId = productId
                });

                await _context.SaveChangesAsync();

                // Liczymy ulubione produkty użytkownika.
                var favoritesCount = await _context.FavoriteProducts
                    .CountAsync(f => f.UserId == userId);

                // Pierwszy dodany produkt.
                if (favoritesCount >= 1)
                {
                    await AwardBadgeIfNeededAsync(
                        userId,
                        "Miłość od pierwszego wejrzenia");
                }

                // Dziesięć dodanych produktów.
                if (favoritesCount >= 10)
                {
                    await AwardBadgeIfNeededAsync(
                        userId,
                        "Zakochany w produktach");
                }

                return true;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogWarning(
                    ex,
                    "[ProductRepository] DbUpdateException in ToggleFavoriteAsync");

                return await _context.FavoriteProducts
                    .AsNoTracking()
                    .AnyAsync(f =>
                        f.UserId == userId &&
                        f.ProductId == productId);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "[ProductRepository] Error occurred in ToggleFavoriteAsync");

                throw;
            }
        }

        // Tylko ogłoszenia, które nadal istnieją (filtr isDeleted działa na Products).
        public async Task<List<int>> GetFavoriteProductIdsAsync(int userId)
        {
            return await _context.Products
                .AsNoTracking()
                .Where(p => _context.FavoriteProducts
                    .Any(f => f.UserId == userId && f.ProductId == p.Id))
                .Select(p => p.Id)
                .ToListAsync();
        }

        // Od ostatnio dodanego do ulubionych.
        public async Task<List<Product>> GetFavoriteProductsAsync(int userId)
        {
            return await (
                from f in _context.FavoriteProducts.AsNoTracking()
                where f.UserId == userId
                join p in _context.Products.AsNoTracking() on f.ProductId equals p.Id
                orderby f.AddedAt descending
                select p
            ).ToListAsync();
        }

        public async Task<List<LocationDTO>> GetLocationsAsync(string query)
        {
            var locations = await _context.Locations
              .Where(x => x.Name.StartsWith(query))
              .OrderBy(x => x.Name)
              .Take(20)
              .Select(x => new LocationDTO
              {
                  Id = x.Id,
                  Name = x.Name
              })
              .ToListAsync();
            return locations ?? new List<LocationDTO>();
        }

        private async Task AwardBadgeIfNeededAsync(int userId, string badgeName)
        {
            // Szukamy odznaki w bazie
            var badge = await _context.Badges
                .FirstOrDefaultAsync(b => b.Name == badgeName);

            if (badge == null)
                return;

            // Sprawdzamy, czy użytkownik już ją zdobył
            var alreadyEarned = await _context.UserBadges
                .AnyAsync(ub =>
                    ub.UserId == userId &&
                    ub.BadgeId == badge.Id);

            if (alreadyEarned)
                return;

            // Przyznajemy odznakę
            _context.UserBadges.Add(new UserBadge
            {
                UserId = userId,
                BadgeId = badge.Id,
                IsEarned = true,
                EarnedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();
        }
    }
}