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
                var product = await _context.Products
                    .Include(p => p.Images)
                    .FirstOrDefaultAsync(p => p.Id == productId && p.UserId == userId);

                if (product == null)
                    return false;

                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ProductRepository] Error occurred in DeleteProductAsync");
                return false;
            }
        }

        public async Task<List<Product>> GetProducts(
            int number = 5,
            ProductCategory? category = null
            )
        {
            try
            {
                var query = _context.Products.AsQueryable();
                if (category.HasValue)
                {
                    query = query.Where(p => p.Category == category.Value);
                }
                return await query
                    .OrderByDescending(p => p.AddedAt)
                    .Take(number)
                    .ToListAsync();

            }
            catch (Exception ex)
            {
                _logger.LogError($"[ProductRepository] Error occured in GetAllProducts : {ex}");
                return new List<Product>();
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
