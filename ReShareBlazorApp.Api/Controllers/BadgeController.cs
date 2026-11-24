using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReShareBlazorApp.Api.Interfaces.Services;
using System.Security.Claims;

namespace ReShareBlazorApp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class BadgeController : ControllerBase
    {
        private readonly IBadgeService _badgeService;

        public BadgeController(IBadgeService badgeService)
        {
            _badgeService = badgeService;
        }

        // Odznaki zalogowanego użytkownika
        [HttpGet("my")]
        public async Task<IActionResult> GetMyBadges()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userId, out var id))
                return Unauthorized();

            var badges = await _badgeService.GetUserBadgesAsync(id);
            return Ok(badges);
        }

        // Odznaki konkretnego użytkownika
        [HttpGet("user/{userId:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetUserBadges(int userId)
        {
            var badges = await _badgeService.GetUserBadgesAsync(userId);
            return Ok(badges);
        }

        // Wszystkie dostępne odznaki - Admin
        // Pobieranie wszystkich odznak - tylko Admin
        [HttpGet("all")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllBadges()
        {
            var badges = await _badgeService.GetAllBadgesAsync();
            return Ok(badges);
        }

        // Ręczne przyznanie odznaki - Admin
        [HttpPost("user/{userId:int}/award/{badgeId:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AwardBadgeToUser(int userId, int badgeId)
        {
            var result = await _badgeService.AwardBadgeToUserAsync(userId, badgeId);

            if (!result)
                return BadRequest("Nie udało się przyznać odznaki.");

            return Ok("Odznaka została przyznana użytkownikowi.");
        }

        // Odebranie odznaki - Admin
        [HttpDelete("user/{userId:int}/remove/{badgeId:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RemoveBadgeFromUser(int userId, int badgeId)
        {
            var result = await _badgeService.RemoveBadgeFromUserAsync(userId, badgeId);

            if (!result)
                return NotFound("Użytkownik nie posiada tej odznaki.");

            return Ok("Odznaka została odebrana użytkownikowi.");
        }
    }
}
