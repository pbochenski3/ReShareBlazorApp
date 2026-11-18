using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
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
                    .FirstOrDefaultAsync(p => p.Id == id);

                return product;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ProductRepository] Error occurred in GetProductById");
                throw;
            }
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