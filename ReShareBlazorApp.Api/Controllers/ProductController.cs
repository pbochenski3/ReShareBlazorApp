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
        [HttpGet]
        public async Task<ActionResult<List<ProductDTO>>> GetProducts([FromQuery] ProductRequest request)
        {
            var products = await _productService.GetProductsAsync(request.Number, request.Category);
            return Ok(products);
        }

    }
}
