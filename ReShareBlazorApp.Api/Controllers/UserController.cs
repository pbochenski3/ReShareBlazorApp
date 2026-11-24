using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ReShareBlazorApp.Api.Date;
using ReShareBlazorApp.Api.Interfaces.Services;
using ReShareBlazorApp.Shared;
using ReShareBlazorApp.Shared.DTOs;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace ReShareBlazorApp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IActivityLogger _activityLogger;
        private readonly UserManager<User> _userManager;

        public UserController(IUserService userService, IActivityLogger activityLogger, UserManager<User> userManager)
        {
            _userService = userService;
            _activityLogger = activityLogger;
            _userManager = userManager;
        }

        [Authorize]
        [HttpGet("my-status")]
        public async Task<ActionResult<UserStatusDTO>> GetMyStatus()
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return NotFound();

            return Ok(new UserStatusDTO
            {
                Id = user.Id,
                IsActive = user.IsActive,
                LockoutEnd = user.LockoutEnd,
                WarningMessage = user.WarningMessage
            });
        }

        [HttpGet]
        public async Task<ActionResult<bool>> CheckIfUserExistAsync
            ([FromQuery] string username)
        {
            return await _userService.CheckIfUserExistAsync(username);
        }

        // Publiczny profil (bez e-maila).
        [HttpGet("profile/{username}")]
        public async Task<ActionResult<UserProfileDTO>> GetProfile(string username)
        {
            var profile = await _userService.GetPublicProfileAsync(username);
            return profile == null ? NotFound() : Ok(profile);
        }

        // Własny profil (z e-mailem).
        [Authorize]
        [HttpGet("me")]
        public async Task<ActionResult<UserProfileDTO>> GetMe()
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();
            var profile = await _userService.GetMyProfileAsync(userId);
            return profile == null ? NotFound() : Ok(profile);
        }

        [Authorize]
        [HttpPut("me")]
        public async Task<IActionResult> UpdateMe(UpdateProfileRequest request)
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();
            var error = await _userService.UpdateProfileAsync(userId, request);
            if (error == null)
                await _activityLogger.LogAsync(User, ActivityActions.ProfileUpdated, "Zaktualizowano profil (miasto/opis)", "User", userId);
            return error == null ? NoContent() : BadRequest(new { message = error });
        }

        [Authorize]
        [HttpPost("me/avatar")]
        public async Task<IActionResult> UploadAvatar(IFormFile file)
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Nie wybrano pliku." });
            await using var stream = file.OpenReadStream();
            var error = await _userService.SetAvatarAsync(userId, stream, file.ContentType, file.Length);
            if (error == null)
                await _activityLogger.LogAsync(User, ActivityActions.AvatarChanged, "Zmieniono zdjęcie profilowe", "User", userId);
            return error == null ? NoContent() : BadRequest(new { message = error });
        }

        [Authorize]
        [HttpDelete("me/avatar")]
        public async Task<IActionResult> DeleteAvatar()
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();
            var error = await _userService.RemoveAvatarAsync(userId);
            if (error == null)
                await _activityLogger.LogAsync(User, ActivityActions.AvatarRemoved, "Usunięto zdjęcie profilowe", "User", userId);
            return error == null ? NoContent() : BadRequest(new { message = error });
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("all")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _userManager.Users
                .Select(u => new
                {
                    Id = u.Id,
                    Username = u.UserName
                })
                .ToListAsync();

            return Ok(users);
        }

        private bool TryGetUserId(out int userId)
        {
            return int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
        }
    }
}
