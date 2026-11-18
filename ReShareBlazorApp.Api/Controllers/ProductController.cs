using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReShareBlazorApp.Api.Date.Enums;
using ReShareBlazorApp.Api.Interfaces.Services;
using ReShareBlazorApp.Shared.DTOs;
using System.Security.Claims;

namespace ReShareBlazorApp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductController : ControllerBase
    {
        private readonly IProductService _productService;
        public ProductController(IProductService productService)
        {
            _productService = productService;
        }
        [Authorize]
        [HttpPost("addProduct")]
        public async Task<IActionResult> AddProduct(ProductDTO product)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Unauthorized();
            int id = int.Parse(userId);
            product.UserId = id;
            var result = await _productService.AddProductAsync(product);
            if (!result)
            {
                return BadRequest();
            }
            return Ok();
        }
        [HttpGet]
        public async Task<ActionResult<List<ProductDTO>>> GetProducts(
        [FromQuery] int number = 5,
        [FromQuery] ProductCategory? category = null,
        [FromQuery] string? q = null,
        [FromQuery] string? lokalizacja = null,
        [FromQuery] decimal? cenaMin = null,
        [FromQuery] decimal? cenaMax = null,
        [FromQuery] string? sortowanie = null)
        {
            var products = await _productService.GetProductsAsync(
                number,
                category,
                q,
                lokalizacja,
                cenaMin,
                cenaMax,
                sortowanie
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
