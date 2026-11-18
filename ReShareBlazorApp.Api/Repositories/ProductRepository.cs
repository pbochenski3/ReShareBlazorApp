using Microsoft.EntityFrameworkCore;
using ReShareBlazorApp.Api.Date;
using ReShareBlazorApp.Api.Date.Enums;
using ReShareBlazorApp.Api.Interfaces.Repositories;

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
        public async Task<Product?> GetProductById(int id)
        {
            try
            {
                return await _context.Products
                    .Include(p => p.user)
                    .FirstOrDefaultAsync(p => p.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError($"[ProductRepository] Error occured in GetProductById : {ex}");
                return null;
            }
        }
        public async Task<List<Product>> GetProductsByUser(int userId)
        {
            try
            {
                return await _context.Products
                    .Where(p => p.UserId == userId)       // tylko ogłoszenia tego użytkownika
                    .OrderByDescending(p => p.AddedAt)    // najnowsze na górze
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"[ProductRepository] Error occured in GetProductsByUser : {ex}");
                return new List<Product>();
            }
        }
        public async Task<List<Product>> GetProducts(
            int number = 5,
            List<ProductCategory>? categories = null,
            ProductSort sort = ProductSort.Newest
            )
        {
            try
            {
                var query = _context.Products.AsQueryable();
                if (categories is { Count: > 0 })
                {
                    query = query.Where(p => categories.Contains(p.Category));
                }
                query = sort switch
                {
                    ProductSort.PriceAsc => query.OrderBy(p => p.Price).ThenByDescending(p => p.AddedAt),
                    ProductSort.PriceDesc => query.OrderByDescending(p => p.Price).ThenByDescending(p => p.AddedAt),
                    _ => query.OrderByDescending(p => p.AddedAt),
                };
                return await query
                    .Take(number)
                    .ToListAsync();
               
            }
            catch (Exception ex)
            {
                _logger.LogError($"[ProductRepository] Error occured in GetAllProducts : {ex}");
                return new List<Product>();
            }
        }
    }
}
