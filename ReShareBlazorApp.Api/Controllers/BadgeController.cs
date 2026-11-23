using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReShareBlazorApp.Api.Interfaces.Services;
using ReShareBlazorApp.Shared.DTOs;
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

        // Pobieranie odznak zalogowanego użytkownika
        [HttpGet("my")]
        public async Task<IActionResult> GetMyBadges()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
                return Unauthorized();

            int id = int.Parse(userId);

            var badges = await _badgeService.GetUserBadgesAsync(id);

            return Ok(badges);
        }

        [HttpGet("user/{userId:int}")]
        public async Task<IActionResult> GetUserBadges(int userId)
        {
            var badges = await _badgeService.GetUserBadgesAsync(userId);
            return Ok(badges);
        }

        // Pobieranie wszystkich odznak - tylko Admin
        [HttpGet("all")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllBadges()
        {
            var badges = await _badgeService.GetAllBadgesAsync();

            return Ok(badges);
        }

        // Dodawanie odznaki - tylko Admin
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddBadge([FromBody] BadgeDTO badge)
        {
            if (badge == null)
                return BadRequest("Nieprawidłowe dane.");

            if (string.IsNullOrWhiteSpace(badge.Name) ||
                string.IsNullOrWhiteSpace(badge.Description) ||
                string.IsNullOrWhiteSpace(badge.Icon))
            {
                return BadRequest("Wszystkie pola są wymagane.");
            }

            var result = await _badgeService.AddBadgeAsync(badge);

            if (!result)
                return BadRequest("Nie udało się dodać odznaki.");

            return Ok("Odznaka została dodana.");
        }
    }
}
