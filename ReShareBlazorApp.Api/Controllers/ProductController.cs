using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
            if (!int.TryParse(userId, out var id))
                return Unauthorized();
            product.UserId = id;
            var imagesError = _productService.ValidateImages(product.Images);
            if (imagesError != null)
                return BadRequest(imagesError);
            var result = await _productService.AddProductAsync(product);
            if (!result)
            {
                return BadRequest();
            }
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

            return NoContent();
        }

        [HttpGet]
        public async Task<ActionResult<List<ProductDTO>>> GetProducts([FromQuery] ProductRequest request)
        {
            var products = await _productService.GetProductsAsync(request.Number, request.Category);
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
