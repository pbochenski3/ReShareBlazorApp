
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

        // Pobieranie wszystkich użytkowników
        [HttpGet("users")]
        public async Task<ActionResult<List<AdminUserDTO>>> GetUsers()
        {
            var users = await _context.Users
                .Where(u => !u.IsDeleted)
                .OrderByDescending(u => u.CreatedAt)
                .Select(u => new AdminUserDTO
                {
                    Id = u.Id,
                    UserName = u.UserName ?? "",
                    Email = u.Email ?? "",
                    PhoneNumber = u.PhoneNumber,
                    City = u.City,
                    Bio = u.Bio,
                    AvatarUrl = string.IsNullOrEmpty(u.AvatarPath)
                        ? null
                        : $"/avatars/{u.AvatarPath}",
                    IsActive = u.IsActive,
                    LockoutEnd = u.LockoutEnd,
                    WarningMessage = u.WarningMessage,
                    CreatedAt = u.CreatedAt
                })
                .ToListAsync();

            return Ok(users);
        }

        // Pobieranie użytkownika po ID
        [HttpGet("users/{id:int}")]
        public async Task<ActionResult<AdminUserDTO>> GetUser(int id)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);

            if (user == null)
                return NotFound(new { message = "Nie znaleziono użytkownika." });

            return Ok(new AdminUserDTO
            {
                Id = user.Id,
                UserName = user.UserName ?? "",
                Email = user.Email ?? "",
                PhoneNumber = user.PhoneNumber,
                City = user.City,
                Bio = user.Bio,
                AvatarUrl = string.IsNullOrEmpty(user.AvatarPath)
                    ? null
                    : $"/avatars/{user.AvatarPath}",
                IsActive = user.IsActive,
                LockoutEnd = user.LockoutEnd,
                WarningMessage = user.WarningMessage,
                CreatedAt = user.CreatedAt
            });
        }

        // Edytowanie użytkownika
        [HttpPut("users/{id:int}")]
        public async Task<IActionResult> UpdateUser(
            int id,
            [FromBody] AdminUpdateUserDTO request)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);

            if (user == null)
                return NotFound(new { message = "Nie znaleziono użytkownika." });

            // Zmiana nazwy użytkownika
            if (!string.Equals(
                user.UserName,
                request.UserName,
                StringComparison.OrdinalIgnoreCase))
            {
                var usernameResult = await _userManager.SetUserNameAsync(
                    user,
                    request.UserName);

                if (!usernameResult.Succeeded)
                {
                    var msg = string.Join("; ",
                        usernameResult.Errors.Select(e => e.Description));

                    return BadRequest(new { message = msg });
                }
            }

            // Zmiana adresu e-mail
            if (!string.Equals(
                user.Email,
                request.Email,
                StringComparison.OrdinalIgnoreCase))
            {
                var emailResult = await _userManager.SetEmailAsync(
                    user,
                    request.Email);

                if (!emailResult.Succeeded)
                {
                    var msg = string.Join("; ",
                        emailResult.Errors.Select(e => e.Description));

                    return BadRequest(new { message = msg });
                }
            }

            user.PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber)
                ? null
                : request.PhoneNumber.Trim();

            user.City = string.IsNullOrWhiteSpace(request.City)
                ? null
                : request.City.Trim();

            user.Bio = string.IsNullOrWhiteSpace(request.Bio)
                ? null
                : request.Bio.Trim();

            // Administrator nie może dezaktywować własnego konta
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (currentUserId != id.ToString())
            {
                user.IsActive = request.IsActive;
            }

            var updateResult = await _userManager.UpdateAsync(user);

            if (!updateResult.Succeeded)
            {
                var msg = string.Join("; ",
                    updateResult.Errors.Select(e => e.Description));

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
                AvatarUrl = string.IsNullOrEmpty(user.AvatarPath)
                    ? null
                    : $"/avatars/{user.AvatarPath}",
                IsActive = user.IsActive,
                LockoutEnd = user.LockoutEnd,
                WarningMessage = user.WarningMessage,
                CreatedAt = user.CreatedAt
            });
        }

        // Statystyki panelu administratora
        [HttpGet("stats")]
        public async Task<ActionResult<AdminStatsDTO>> GetStats()
        {
            var weekAgo = DateTime.UtcNow.AddDays(-7);
            var now = DateTimeOffset.UtcNow;

            return Ok(new AdminStatsDTO
            {
                TotalUsers = await _context.Users
                    .CountAsync(u => !u.IsDeleted),

                ActiveUsers = await _context.Users
                    .CountAsync(u =>
                        !u.IsDeleted &&
                        u.IsActive &&
                        (!u.LockoutEnd.HasValue || u.LockoutEnd.Value <= now)),

                TotalProducts = await _context.Products
                    .CountAsync(p => !p.isDeleted),

                ProductsLast7Days = await _context.Products
                    .CountAsync(p =>
                        !p.isDeleted &&
                        p.AddedAt >= weekAgo)
            });
        }

        // Dodawanie zdjęcia profilowego
        [HttpPost("users/{id:int}/avatar")]
        public async Task<IActionResult> UploadUserAvatar(
            int id,
            IFormFile file)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);

            if (user == null)
                return NotFound(new { message = "Nie znaleziono użytkownika." });

            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Nie wybrano pliku." });

            await using var stream = file.OpenReadStream();

            var error = await _userService.SetAvatarAsync(
                id,
                stream,
                file.ContentType,
                file.Length);

            if (error != null)
                return BadRequest(new { message = error });

            var updatedUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id);

            var avatarUrl = string.IsNullOrEmpty(updatedUser?.AvatarPath)
                ? null
                : $"/avatars/{updatedUser.AvatarPath}";

            return Ok(new { avatarUrl });
        }

        // Usuwanie zdjęcia profilowego
        [HttpDelete("users/{id:int}/avatar")]
        public async Task<IActionResult> DeleteUserAvatar(int id)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);

            if (user == null)
                return NotFound(new { message = "Nie znaleziono użytkownika." });

            var error = await _userService.RemoveAvatarAsync(id);

            return error == null
                ? NoContent()
                : BadRequest(new { message = error });
        }

        // Aktywacja lub dezaktywacja użytkownika
        [HttpPost("users/{id:int}/active")]
        public async Task<IActionResult> SetUserActive(
            int id,
            [FromQuery] bool value)
        {
            if (User.FindFirstValue(ClaimTypes.NameIdentifier) == id.ToString())
                return BadRequest(new
                {
                    message = "Nie możesz dezaktywować własnego konta."
                });

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);

            if (user == null)
                return NotFound(new { message = "Nie znaleziono użytkownika." });

            user.IsActive = value;

            if (value)
                user.LockoutEnd = null;

            await _context.SaveChangesAsync();

            return Ok();
        }

        // Tymczasowa blokada użytkownika
        [HttpPost("users/{id:int}/temporary-block")]
        public async Task<IActionResult> SetTemporaryBlock(
            int id,
            [FromBody] TemporaryBlockRequest request)
        {
            if (User.FindFirstValue(ClaimTypes.NameIdentifier) == id.ToString())
                return BadRequest(new
                {
                    message = "Nie możesz zablokować samego siebie."
                });

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);

            if (user == null)
                return NotFound(new { message = "Nie znaleziono użytkownika." });

            DateTimeOffset targetDate;

            if (request.BlockedUntil.HasValue &&
                request.BlockedUntil.Value > DateTimeOffset.UtcNow)
            {
                targetDate = request.BlockedUntil.Value;
            }
            else if (request.Hours > 0)
            {
                targetDate = DateTimeOffset.UtcNow.AddHours(request.Hours);
            }
            else
            {
                return BadRequest(new
                {
                    message = "Podaj prawidłowy czas blokady."
                });
            }

            user.LockoutEnabled = true;
            user.LockoutEnd = targetDate;

            await _context.SaveChangesAsync();

            return Ok(new { lockoutEnd = user.LockoutEnd });
        }

        // Odblokowanie użytkownika
        [HttpPost("users/{id:int}/unblock")]
        public async Task<IActionResult> UnblockUser(int id)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);

            if (user == null)
                return NotFound(new { message = "Nie znaleziono użytkownika." });

            user.IsActive = true;
            user.LockoutEnd = null;

            await _context.SaveChangesAsync();

            return Ok();
        }

        // Wysłanie ostrzeżenia użytkownikowi
        [HttpPost("users/{id:int}/warn")]
        public async Task<IActionResult> WarnUser(
            int id,
            [FromBody] WarnUserRequest request)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);

            if (user == null)
                return NotFound(new { message = "Nie znaleziono użytkownika." });

            user.WarningMessage = string.IsNullOrWhiteSpace(request.Message)
                ? "Twoje konto narusza zasady serwisu. W przypadku dalszych naruszeń regulaminu zostanie zablokowane."
                : request.Message.Trim();

            await _context.SaveChangesAsync();

            return Ok(new { warningMessage = user.WarningMessage });
        }

        // Usunięcie ostrzeżenia
        [HttpPost("users/{id:int}/clear-warning")]
        public async Task<IActionResult> ClearWarning(int id)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);

            if (user == null)
                return NotFound(new { message = "Nie znaleziono użytkownika." });

            user.WarningMessage = null;

            await _context.SaveChangesAsync();

            return Ok();
        }

        // Soft delete użytkownika
        [HttpDelete("users/{id:int}")]
        public async Task<IActionResult> SoftDeleteUser(int id)
        {
            if (User.FindFirstValue(ClaimTypes.NameIdentifier) == id.ToString())
                return BadRequest(new
                {
                    message = "Nie możesz usunąć własnego konta administratora."
                });

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);

            if (user == null)
                return NotFound(new { message = "Nie znaleziono użytkownika." });

            user.IsDeleted = true;
            user.IsActive = false;

            var products = await _context.Products
                .Where(p => p.UserId == id && !p.isDeleted)
                .ToListAsync();

            foreach (var product in products)
            {
                product.isDeleted = true;
            }

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // Pobieranie ogłoszeń
        [HttpGet("products")]
        public async Task<ActionResult<List<ProductDTO>>> GetProducts()
        {
            var products = await _context.Products
                .Where(p => !p.isDeleted)
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

        // Usuwanie ogłoszenia przez administratora
        [HttpDelete("products/{id:int}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == id && !p.isDeleted);

            if (product == null)
                return NotFound(new
                {
                    message = "Nie znaleziono ogłoszenia."
                });

            product.isDeleted = true;

            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
