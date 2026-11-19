using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReShareBlazorApp.Api.Date;
using ReShareBlazorApp.Api.Interfaces.Services;
using ReShareBlazorApp.Shared;
using ReShareBlazorApp.Shared.DTOs;
using System.Security.Claims;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly ReshareDbContext _context;
    private readonly IActivityLogger _activityLogger;

    public AdminController(ReshareDbContext context, IActivityLogger activityLogger)
    {
        _context = context;
        _activityLogger = activityLogger;
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

        await _activityLogger.LogAsync(User,
            value ? ActivityActions.AdminUserUnblocked : ActivityActions.AdminUserBlocked,
            $"{(value ? "Odblokowano" : "Zablokowano")} użytkownika {user.UserName} (id: {id})",
            "User", id);
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

        var productName = product.Name;
        _context.Products.Remove(product);
        await _context.SaveChangesAsync();

        await _activityLogger.LogAsync(User, ActivityActions.AdminProductDeleted,
            $"Usunięto ogłoszenie „{productName}” (id: {id})", "Product", id);
        return NoContent();
    }

    // GET api/admin/logs?user=jan&action=ProductAdded&page=1&pageSize=25
    [HttpGet("logs")]
    public async Task<ActionResult<ActivityLogPageDTO>> GetLogs(
        [FromQuery] string? user = null,
        [FromQuery(Name = "action")] string? actionCode = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _context.ActivityLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(user))
        {
            var term = user.Trim();
            query = query.Where(l => l.UserName.Contains(term));
        }
        if (!string.IsNullOrWhiteSpace(actionCode))
            query = query.Where(l => l.Action == actionCode);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(l => l.CreatedAt)
            .ThenByDescending(l => l.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new ActivityLogDTO
            {
                Id = l.Id,
                UserId = l.UserId,
                UserName = l.UserName,
                Action = l.Action,
                Description = l.Description,
                TargetType = l.TargetType,
                TargetId = l.TargetId,
                CreatedAt = l.CreatedAt
            })
            .ToListAsync();

        return Ok(new ActivityLogPageDTO
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        });
    }
}