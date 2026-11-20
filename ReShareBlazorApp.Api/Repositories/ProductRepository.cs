using Microsoft.EntityFrameworkCore;
using ReShareBlazorApp.Api.Date;
using ReShareBlazorApp.Api.Date.Enums;
using ReShareBlazorApp.Api.Interfaces.Repositories;
using ReShareBlazorApp.Shared.DTOs;

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
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"[ProductRepository] Error occured in AddProductAsync : {ex}");
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
                existingProduct.Price = product.Price;
                existingProduct.ExpiresAt = product.ExpiresAt;
                existingProduct.Type = product.Type;
                existingProduct.Category = product.Category;
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
                // Zwykłe usunięcie = ogłoszenie znika z bazy na stałe (powiązane zdjęcia i zgłoszenia
                // kasują się kaskadowo). Dzięki temu NIE zwiększa licznika "odratowanych" rzeczy.
                var rows = await _context.Products
                    .Where(p => p.Id == productId && p.UserId == userId)
                    .ExecuteDeleteAsync();
                return rows > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ProductRepository] Error occurred in DeleteProductAsync");
                return false;
            }
        }

        // Oznaczenie jako sprzedane: isDeleted = true (ogłoszenie znika ze strony, a wiersz zostaje
        // w bazie i jest liczony przez licznik na stronie głównej).
        public async Task<bool> MarkAsSoldAsync(int productId, int userId)
        {
            try
            {
                var rows = await _context.Products
                    .Where(p => p.Id == productId && p.UserId == userId)
                    .ExecuteUpdateAsync(p => p.SetProperty(x => x.isDeleted, true));
                return rows > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ProductRepository] Error occurred in MarkAsSoldAsync");
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

        public async Task<List<Product>> GetProducts(
    int number = 5,
    ProductCategory? category = null,
    string? search = null,
    string? location = null,
    decimal? minPrice = null,
    decimal? maxPrice = null,
    string? sort = null)
        {
            try
            {
                var query = _context.Products.AsNoTracking().AsQueryable();
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
                    query = query.Where(p => p.Price >= minPrice.Value);
                }
                if (maxPrice.HasValue)
                {
                    query = query.Where(p => p.Price <= maxPrice.Value);
                }
                query = sort switch
                {
                    "cenaRosnaco" => query.OrderBy(p => p.Price),

                    "cenaMalejaco" => query.OrderByDescending(p => p.Price),

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
    }
}