using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReShareBlazorApp.Api.Date;
using ReShareBlazorApp.Shared.DTOs;
using System.Security.Claims;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly ReshareDbContext _context;

    public AdminController(ReshareDbContext context)
    {
        _context = context;
    }

    [HttpGet("users")]
    public async Task<ActionResult<List<AdminUserDTO>>> GetUsers()
    {
        var users = await _context.Users
            .Select(u => new AdminUserDTO
            {
                Id = u.Id,
                UserName = u.UserName,
                Email = u.Email,
                IsActive = u.IsActive,
            })
            .ToListAsync();
        return Ok(users);
    }

    [HttpGet("stats")]
    public async Task<ActionResult<AdminStatsDTO>> GetStats()
    {
        var weekAgo = DateTime.Now.AddDays(-7);
        return Ok(new AdminStatsDTO
        {
            TotalUsers = await _context.Users.CountAsync(),
            ActiveUsers = await _context.Users.CountAsync(u => u.IsActive),
            TotalProducts = await _context.Products.CountAsync(),
            ProductsLast7Days = await _context.Products.CountAsync(p => p.AddedAt >= weekAgo)
        });
    }

    [HttpPost("users/{id:int}/active")]
    public async Task<IActionResult> SetUserActive(int id, [FromQuery] bool value)
    {
        if (User.FindFirstValue(ClaimTypes.NameIdentifier) == id.ToString())
            return BadRequest();   // nie blokuj samego siebie

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) return NotFound();

        user.IsActive = value;
        await _context.SaveChangesAsync();
        return Ok();
    }

    [HttpGet("products")]
    public async Task<ActionResult<List<ProductDTO>>> GetProducts()
    {
        var products = await _context.Products
            .OrderByDescending(p => p.AddedAt)
            .Select(p => new ProductDTO
            {
                ProductId = p.Id,
                Name = p.Name,
                Description = p.Description,
                City = p.City,
                Price = p.Price,
                AddedAt = p.AddedAt,
                ExpiresAt = p.ExpiresAt,
                Type = p.Type,
                Category = p.Category,
                UserId = p.UserId,
                AddedBy = p.user.UserName ?? ""
            })
            .ToListAsync();
        return Ok(products);
    }

    [HttpDelete("products/{id:int}")]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id);
        if (product == null) return NotFound();

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}