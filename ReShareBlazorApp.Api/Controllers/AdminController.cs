using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReShareBlazorApp.Api.Date;
using ReShareBlazorApp.Api.Interfaces.Services;
using ReShareBlazorApp.Shared.DTOs;
using System.Security.Claims;

namespace ReShareBlazorApp.Api.Controllers
{
    [ApiController]
    [Route("api/admin")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly ReshareDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IUserService _userService;

        public AdminController(
            ReshareDbContext context,
            UserManager<User> userManager,
            IUserService userService)
        {
            _context = context;
            _userManager = userManager;
            _userService = userService;
        }

        [HttpGet("users")]
        public async Task<ActionResult<List<AdminUserDTO>>> GetUsers()
        {
            var users = await _context.Users
                .OrderByDescending(u => u.CreatedAt)
                .Select(u => new AdminUserDTO
                {
                    Id = u.Id,
                    UserName = u.UserName ?? "",
                    Email = u.Email ?? "",
                    PhoneNumber = u.PhoneNumber,
                    City = u.City,
                    Bio = u.Bio,
                    AvatarUrl = string.IsNullOrEmpty(u.AvatarPath) ? null : $"/avatars/{u.AvatarPath}",
                    IsActive = u.IsActive,
                    CreatedAt = u.CreatedAt
                })
                .ToListAsync();
            return Ok(users);
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

        [HttpGet("users/{id:int}")]
        public async Task<ActionResult<AdminUserDTO>> GetUser(int id)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return NotFound(new { message = "Nie znaleziono użytkownika." });

            return Ok(new AdminUserDTO
            {
                Id = user.Id,
                UserName = user.UserName ?? "",
                Email = user.Email ?? "",
                PhoneNumber = user.PhoneNumber,
                City = user.City,
                Bio = user.Bio,
                AvatarUrl = string.IsNullOrEmpty(user.AvatarPath) ? null : $"/avatars/{user.AvatarPath}",
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt
            });
        }

        [HttpPut("users/{id:int}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] AdminUpdateUserDTO request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return NotFound(new { message = "Nie znaleziono użytkownika." });

            // Zmiana nazwy użytkownika, jeśli inna
            if (!string.Equals(user.UserName, request.UserName, StringComparison.OrdinalIgnoreCase))
            {
                var usernameResult = await _userManager.SetUserNameAsync(user, request.UserName);
                if (!usernameResult.Succeeded)
                {
                    var msg = string.Join("; ", usernameResult.Errors.Select(e => e.Description));
                    return BadRequest(new { message = msg });
                }
            }

            // Zmiana adresu e-mail, jeśli inny
            if (!string.Equals(user.Email, request.Email, StringComparison.OrdinalIgnoreCase))
            {
                var emailResult = await _userManager.SetEmailAsync(user, request.Email);
                if (!emailResult.Succeeded)
                {
                    var msg = string.Join("; ", emailResult.Errors.Select(e => e.Description));
                    return BadRequest(new { message = msg });
                }
            }

            user.PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim();
            user.City = string.IsNullOrWhiteSpace(request.City) ? null : request.City.Trim();
            user.Bio = string.IsNullOrWhiteSpace(request.Bio) ? null : request.Bio.Trim();

            // Administrator nie może zablokować samego siebie
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (currentUserId != id.ToString())
            {
                user.IsActive = request.IsActive;
            }

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                var msg = string.Join("; ", updateResult.Errors.Select(e => e.Description));
                return BadRequest(new { message = msg });
            }

            return Ok(new AdminUserDTO
            {
                Id = user.Id,
                UserName = user.UserName ?? "",
                Email = user.Email ?? "",
                PhoneNumber = user.PhoneNumber,
                City = user.City,
                Bio = user.Bio,
                AvatarUrl = string.IsNullOrEmpty(user.AvatarPath) ? null : $"/avatars/{user.AvatarPath}",
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt
            });
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

        [HttpPost("users/{id:int}/avatar")]
        public async Task<IActionResult> UploadUserAvatar(int id, IFormFile file)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return NotFound(new { message = "Nie znaleziono użytkownika." });

            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Nie wybrano pliku." });

            await using var stream = file.OpenReadStream();
            var error = await _userService.SetAvatarAsync(id, stream, file.ContentType, file.Length);
            if (error != null)
                return BadRequest(new { message = error });

            var updatedUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            var avatarUrl = string.IsNullOrEmpty(updatedUser?.AvatarPath) ? null : $"/avatars/{updatedUser.AvatarPath}";
            return Ok(new { avatarUrl });
        }

        [HttpDelete("users/{id:int}/avatar")]
        public async Task<IActionResult> DeleteUserAvatar(int id)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return NotFound(new { message = "Nie znaleziono użytkownika." });

            var error = await _userService.RemoveAvatarAsync(id);
            return error == null ? NoContent() : BadRequest(new { message = error });
        }

        [HttpPost("users/{id:int}/active")]
        public async Task<IActionResult> SetUserActive(int id, [FromQuery] bool value)
        {
            if (User.FindFirstValue(ClaimTypes.NameIdentifier) == id.ToString())
                return BadRequest(new { message = "Nie możesz zablokować własnego konta." });
    [HttpPost("users/{id:int}/active")]
    public async Task<IActionResult> SetUserActive(int id, [FromQuery] bool value)
    {
        if (User.FindFirstValue(ClaimTypes.NameIdentifier) == id.ToString())
            return BadRequest(new { message = "Nie możesz zablokować samego siebie." });

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return NotFound(new { message = "Nie znaleziono użytkownika." });

            user.IsActive = value;
            await _context.SaveChangesAsync();
            return Ok();
        }
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

        [HttpDelete("users/{id:int}")]
        public async Task<IActionResult> SoftDeleteUser(int id)
        {
            if (User.FindFirstValue(ClaimTypes.NameIdentifier) == id.ToString())
                return BadRequest(new { message = "Nie możesz usunąć własnego konta administratora." });

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return NotFound(new { message = "Nie znaleziono użytkownika." });

            // Soft delete: oznacz jako usunięty i nieaktywny
            user.IsDeleted = true;
            user.IsActive = false;

            // Ukryj (soft delete) również ogłoszenia tego użytkownika
            var products = await _context.Products.Where(p => p.UserId == id).ToListAsync();
            foreach (var product in products)
            {
                product.isDeleted = true;
            }

            await _context.SaveChangesAsync();
            return NoContent();
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
            if (product == null) return NotFound(new { message = "Nie znaleziono ogłoszenia." });

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
