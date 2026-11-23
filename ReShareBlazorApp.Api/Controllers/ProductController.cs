using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReShareBlazorApp.Api.Date;
using ReShareBlazorApp.Api.Date.Enums;
using ReShareBlazorApp.Api.Interfaces.Services;
using ReShareBlazorApp.Shared;
using ReShareBlazorApp.Shared.DTOs;
using ReShareBlazorApp.Shared.Enums;
using System.Security.Claims;

namespace ReShareBlazorApp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductController : ControllerBase
    {
        private readonly IProductService _productService;
        private readonly IActivityLogger _activityLogger;
        public ProductController(IProductService productService, IActivityLogger activityLogger)
        {
            _productService = productService;
            _activityLogger = activityLogger;
        }

        [Authorize]
        [HttpPost("addProduct")]
        public async Task<IActionResult> AddProduct(ProductDTO product)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userId, out var id))
                return Unauthorized();

            product.UserId = id;

            var imagesError = _productService.ValidateImages(product.Images);

            if (imagesError != null)
                return BadRequest(imagesError);

            var result = await _productService.AddProductAsync(product);

            if (!result)
                return BadRequest();

            var userProductsCount = await _context.Products
                .IgnoreQueryFilters()
                .CountAsync(p => p.UserId == id);

            if (userProductsCount == 1)
            {
                var badge = await _context.Badges
                    .FirstOrDefaultAsync(b => b.Name == "Pierwsze ogłoszenie");

                if (badge != null)
                {
                    var alreadyEarned = await _context.UserBadges
                        .AnyAsync(ub =>
                            ub.UserId == id &&
                            ub.BadgeId == badge.Id);

                    if (!alreadyEarned)
                    {
                        _context.UserBadges.Add(new UserBadge
                        {
                            UserId = id,
                            BadgeId = badge.Id,
                            IsEarned = true,
                            EarnedAt = DateTime.Now
                        });

                        await _context.SaveChangesAsync();
                    }
                }
            }

            var userProductsCountFor5Badge = await _context.Products
                .CountAsync(p => p.UserId == id);

            if (userProductsCountFor5Badge >= 5)
            {
                var badge = await _context.Badges
                    .FirstOrDefaultAsync(b => b.Name == "Aktywny sprzedawca");

                if (badge != null)
                {
                    var alreadyEarned = await _context.UserBadges
                        .AnyAsync(ub => ub.UserId == id && ub.BadgeId == badge.Id);

                    if (!alreadyEarned)
                    {
                        _context.UserBadges.Add(new UserBadge
                        {
                            UserId = id,
                            BadgeId = badge.Id,
                            IsEarned = true,
                            EarnedAt = DateTime.Now
                        });

                        await _context.SaveChangesAsync();
                    }
                }
            }
            await _activityLogger.LogAsync(User, ActivityActions.ProductAdded,
                $"Dodano ogłoszenie „{product.Name}”", "Product");
            return Ok();
        }


        [Authorize]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateProduct(int id, ProductUpdateDTO product)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userId, out var currentUserId))
                return Unauthorized();

            var result = await _productService.UpdateProductAsync(id, currentUserId, product);
            if (!result)
                return NotFound();

            await _activityLogger.LogAsync(User, ActivityActions.ProductUpdated,
                $"Zmieniono ogłoszenie „{product.Name}” (id: {id})", "Product", id);

            return NoContent();
        }

        [Authorize]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userId, out var currentUserId))
                return Unauthorized();

            var result = await _productService.DeleteProductAsync(id, currentUserId);
            if (!result)
                return NotFound();

            await _activityLogger.LogAsync(User, ActivityActions.ProductDeleted,
                $"Usunięto ogłoszenie (id: {id})", "Product", id);

            return NoContent();
        }

        [HttpGet]
        public async Task<ActionResult<List<ProductDTO>>> GetProducts(
        [FromQuery] int number = 5,
        [FromQuery] ProductCategory? category = null,
        [FromQuery] string? q = null,
        [FromQuery] string? lokalizacja = null,
        [FromQuery] decimal? cenaMin = null,
        [FromQuery] decimal? cenaMax = null,
         [FromQuery] string? sortowanie = null,
         [FromQuery] int? userId = null,
         [FromQuery] ProductType? type = null)
        {
            var products = await _productService.GetProductsAsync(
                number,
                category,
                q,
                lokalizacja,
                cenaMin,
                cenaMax,
                sortowanie,
                userId,
                type
            );

            return Ok(products);
        }
        [HttpGet("{id:int}")]
        public async Task<ActionResult<ProductDTO>> GetProduct(int id)
        {
            var product = await _productService.GetProductById(id);

            if (product == null)
                return NotFound();

            return Ok(product);
        }
        [HttpGet("image/{id:int}")]
        [ResponseCache(Duration = 86400, Location = ResponseCacheLocation.Any)]
        public async Task<IActionResult> GetImage(int id)
        {
            var image = await _productService.GetImageAsync(id);

            if (image == null)
                return NotFound();

            return File(image.Data, image.ContentType);
        }
        [HttpGet("searchLocations")]
        public async Task<ActionResult<List<LocationDTO>>> SearchLocations(
          [FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 2)
                return Ok(new List<LocationDTO>());
            var locations = await _productService.GetLocationsAsync(query);
            return Ok(locations);
        }

    }
}
