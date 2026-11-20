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
                UserName = u.UserName ?? "",
                Email = u.Email ?? "",
                IsActive = u.IsActive,
                LockoutEnd = u.LockoutEnd,
                WarningMessage = u.PhoneNumber
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
            ActiveUsers = await _context.Users.CountAsync(u => u.IsActive && (!u.LockoutEnd.HasValue || u.LockoutEnd.Value <= DateTimeOffset.UtcNow)),
            TotalProducts = await _context.Products.CountAsync(),
            ProductsLast7Days = await _context.Products.CountAsync(p => p.AddedAt >= weekAgo)
        });
    }

    [HttpPost("users/{id:int}/active")]
    public async Task<IActionResult> SetUserActive(int id, [FromQuery] bool value)
    {
        if (User.FindFirstValue(ClaimTypes.NameIdentifier) == id.ToString())
            return BadRequest(new { message = "Nie możesz zablokować samego siebie." });

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) return NotFound();

        user.IsActive = value;
        if (value)
        {
            user.LockoutEnd = null;
        }
        await _context.SaveChangesAsync();
        return Ok();
    }

    [HttpPost("users/{id:int}/temporary-block")]
    public async Task<IActionResult> SetTemporaryBlock(int id, [FromBody] TemporaryBlockRequest request)
    {
        if (User.FindFirstValue(ClaimTypes.NameIdentifier) == id.ToString())
            return BadRequest(new { message = "Nie możesz zablokować samego siebie." });

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) return NotFound();

        DateTimeOffset targetDate;
        if (request.BlockedUntil.HasValue && request.BlockedUntil.Value > DateTimeOffset.UtcNow)
        {
            targetDate = request.BlockedUntil.Value;
        }
        else if (request.Hours > 0)
        {
            targetDate = DateTimeOffset.UtcNow.AddHours(request.Hours);
        }
        else
        {
            return BadRequest(new { message = "Podaj prawidłowy czas blokady." });
        }

        user.LockoutEnabled = true;
        user.LockoutEnd = targetDate;
        await _context.SaveChangesAsync();

        return Ok(new { lockoutEnd = user.LockoutEnd });
    }

    [HttpPost("users/{id:int}/unblock")]
    public async Task<IActionResult> UnblockUser(int id)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) return NotFound();

        user.IsActive = true;
        user.LockoutEnd = null;
        await _context.SaveChangesAsync();
        return Ok();
    }

    [HttpPost("users/{id:int}/warn")]
    public async Task<IActionResult> WarnUser(int id, [FromBody] WarnUserRequest request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) return NotFound();

        user.PhoneNumber = string.IsNullOrWhiteSpace(request.Message)
            ? "Twoje konto narusza zasady serwisu. W przypadku dalszych naruszeń regulaminu zostanie zablokowane."
            : request.Message.Trim();

        await _context.SaveChangesAsync();
        return Ok(new { warningMessage = user.PhoneNumber });
    }

    [HttpPost("users/{id:int}/clear-warning")]
    public async Task<IActionResult> ClearWarning(int id)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) return NotFound();

        user.PhoneNumber = null;
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