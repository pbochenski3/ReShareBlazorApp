<<<<<<< HEAD
﻿
using Microsoft.AspNetCore.Authorization;
=======
﻿using Microsoft.AspNetCore.Authorization;
>>>>>>> origin/JHubisz
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReShareBlazorApp.Api.Date;
using ReShareBlazorApp.Api.Interfaces.Services;
using ReShareBlazorApp.Shared.DTOs;
using System.Security.Claims;

namespace ReShareBlazorApp.Api.Controllers
{
<<<<<<< HEAD
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
=======
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
>>>>>>> origin/JHubisz
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

<<<<<<< HEAD
        // Pobieranie użytkownika po ID
        [HttpGet("users/{id:int}")]
        public async Task<ActionResult<AdminUserDTO>> GetUser(int id)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);
=======
        if (string.Equals(user.UserName, "admin12345", StringComparison.OrdinalIgnoreCase) && !value)
        {
            return BadRequest(new { message = "Główny administrator nie może zostać zablokowany." });
        }

        user.IsActive = value;
        await _context.SaveChangesAsync();
        return Ok();
    }
>>>>>>> origin/JHubisz

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
