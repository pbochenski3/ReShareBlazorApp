using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
        // GET api/product/mine -> ogłoszenia zalogowanego użytkownika
        [Authorize]
        [HttpGet("mine")]
        public async Task<ActionResult<List<ProductDTO>>> GetMyProducts()
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return Unauthorized();
            return Ok(await _productService.GetUserProductsAsync(userId));
        }
        [HttpGet("{id:int}")]
        public async Task<ActionResult<ProductDetailsDTO>> GetProduct(int id)
        {
            var product = await _productService.GetProductByIdAsync(id);
            if (product == null)
                return NotFound();
            return Ok(product);
        }
        [HttpGet]
        public async Task<ActionResult<List<ProductDTO>>> GetProducts([FromQuery] ProductRequest request)
        {
            var products = await _productService.GetProductsAsync(request.Number, request.Categories, request.Sort);
            return Ok(products);
        }

    }
}
