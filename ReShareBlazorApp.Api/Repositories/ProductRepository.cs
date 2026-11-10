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
        public async Task<List<Product>> GetProducts(
            int number = 5,
            ProductCategory? category = null
            )
        {
            try
            {
                var query = _context.Products.AsQueryable();
                if(category.HasValue)
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
    }
}
