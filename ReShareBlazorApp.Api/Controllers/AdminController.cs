using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<IdentityRole<int>> _roleManager;

    public AdminController(
        ReshareDbContext context,
        UserManager<User> userManager,
        RoleManager<IdentityRole<int>> roleManager)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    [HttpGet("users")]
    public async Task<ActionResult<List<AdminUserDTO>>> GetUsers()
    {
        var users = await _context.Users
            .OrderBy(u => u.Id)
            .ToListAsync();

        var adminRole = await _roleManager.FindByNameAsync("Admin")
            ?? await _context.Roles.FirstOrDefaultAsync(r => r.NormalizedName == "ADMIN" || r.Name == "Admin");

        var adminRoleId = adminRole?.Id ?? 0;
        var adminUserIds = adminRoleId > 0
            ? await _context.UserRoles
                .Where(ur => ur.RoleId == adminRoleId)
                .Select(ur => ur.UserId)
                .ToHashSetAsync()
            : new HashSet<int>();

        var result = users.Select(u => new AdminUserDTO
        {
            Id = u.Id,
            UserName = u.UserName ?? "",
            Email = u.Email ?? "",
            IsActive = u.IsActive,
            IsAdmin = adminUserIds.Contains(u.Id)
        }).ToList();

        return Ok(result);
    }

    [HttpPost("users")]
    public async Task<IActionResult> CreateUser([FromBody] AdminCreateUserRequest request)
    {
        if (request == null)
            return BadRequest(new { message = "Brak danych użytkownika." });

        if (string.IsNullOrWhiteSpace(request.UserName) || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { message = "Nazwa użytkownika, e-mail i hasło są wymagane." });

        var normalizedEmail = request.Email.Trim();
        var normalizedUserName = request.UserName.Trim();

        if (await _userManager.FindByEmailAsync(normalizedEmail) != null)
            return BadRequest(new { message = "Użytkownik o podanym adresie e-mail już istnieje." });

        if (await _userManager.FindByNameAsync(normalizedUserName) != null)
            return BadRequest(new { message = "Użytkownik o podanej nazwie już istnieje." });

        var user = new User
        {
            UserName = normalizedUserName,
            Email = normalizedEmail,
            EmailConfirmed = true,
            CreatedAt = DateTime.Now,
            IsActive = true
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join(" ", result.Errors.Select(e => e.Description));
            return BadRequest(new { message = errors });
        }

        if (request.IsAdmin)
        {
            if (!await _roleManager.RoleExistsAsync("Admin"))
            {
                await _roleManager.CreateAsync(new IdentityRole<int>("Admin"));
            }
            var roleResult = await _userManager.AddToRoleAsync(user, "Admin");
            if (!roleResult.Succeeded)
            {
                var errors = string.Join(" ", roleResult.Errors.Select(e => e.Description));
                return BadRequest(new { message = errors });
            }
        }

        return Ok(new AdminUserDTO
        {
            Id = user.Id,
            UserName = user.UserName,
            Email = user.Email,
            IsActive = user.IsActive,
            IsAdmin = request.IsAdmin
        });
    }

    [HttpPost("users/{id:int}/role")]
    public async Task<IActionResult> SetUserRole(int id, [FromQuery] bool isAdmin)
    {
        var currentUserIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (currentUserIdStr == id.ToString() && !isAdmin)
        {
            return BadRequest(new { message = "Nie możesz odebrać sobie uprawnień administratora." });
        }

        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null) return NotFound(new { message = "Nie znaleziono użytkownika." });

        // Ochrona głównego konta administratora
        if (string.Equals(user.UserName, "admin12345", StringComparison.OrdinalIgnoreCase) && !isAdmin)
        {
            return BadRequest(new { message = "Główny administrator (admin12345) nie może zostać pozbawiony uprawnień." });
        }

        if (!await _roleManager.RoleExistsAsync("Admin"))
        {
            await _roleManager.CreateAsync(new IdentityRole<int>("Admin"));
        }

        var adminRole = await _roleManager.FindByNameAsync("Admin")
            ?? await _context.Roles.FirstOrDefaultAsync(r => r.NormalizedName == "ADMIN" || r.Name == "Admin");

        if (isAdmin)
        {
            if (!await _userManager.IsInRoleAsync(user, "Admin"))
            {
                var addResult = await _userManager.AddToRoleAsync(user, "Admin");
                if (!addResult.Succeeded)
                {
                    return BadRequest(new { message = string.Join(" ", addResult.Errors.Select(e => e.Description)) });
                }
            }
        }
        else
        {
            // Sprawdzenie, czy nie usuwamy ostatniego administratora
            var adminRoleId = adminRole?.Id ?? 0;
            var adminCount = adminRoleId > 0
                ? await _context.UserRoles.CountAsync(ur => ur.RoleId == adminRoleId)
                : 0;

            if (adminCount <= 1 && await _userManager.IsInRoleAsync(user, "Admin"))
            {
                return BadRequest(new { message = "W systemie musi pozostać co najmniej jeden administrator." });
            }

            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                var removeResult = await _userManager.RemoveFromRoleAsync(user, "Admin");
                if (!removeResult.Succeeded)
                {
                    return BadRequest(new { message = string.Join(" ", removeResult.Errors.Select(e => e.Description)) });
                }
            }
        }

        // Aktualizacja SecurityStamp, aby unieważnić stare sesje
        await _userManager.UpdateSecurityStampAsync(user);

        return Ok();
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

        if (string.Equals(user.UserName, "admin12345", StringComparison.OrdinalIgnoreCase) && !value)
        {
            return BadRequest(new { message = "Główny administrator nie może zostać zablokowany." });
        }

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
