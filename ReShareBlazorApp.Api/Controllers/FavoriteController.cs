using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReShareBlazorApp.Api.Interfaces.Services;
using ReShareBlazorApp.Shared.DTOs;
using System.Security.Claims;

namespace ReShareBlazorApp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // wszystkie akcje wymagają zalogowania (token JWT)
    public class FavoriteController : ControllerBase
    {
        private readonly IFavoriteService _favoriteService;
        public FavoriteController(IFavoriteService favoriteService)
        {
            _favoriteService = favoriteService;
        }

        // Id zalogowanego użytkownika wyciągamy z tokena
        private int? CurrentUserId =>
            int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

        // GET api/favorite -> pełne ogłoszenia z ulubionych
        [HttpGet]
        public async Task<ActionResult<List<ProductDTO>>> GetFavorites()
        {
            if (CurrentUserId is not int userId) return Unauthorized();
            return Ok(await _favoriteService.GetFavoritesAsync(userId));
        }

        // GET api/favorite/ids -> same numery ogłoszeń (do kolorowania serduszek)
        [HttpGet("ids")]
        public async Task<ActionResult<List<int>>> GetIds()
        {
            if (CurrentUserId is not int userId) return Unauthorized();
            return Ok(await _favoriteService.GetIdsAsync(userId));
        }

        // POST api/favorite/5/toggle -> dodaje albo usuwa z ulubionych
        [HttpPost("{productId:int}/toggle")]
        public async Task<ActionResult<FavoriteToggleResponse>> Toggle(int productId)
        {
            if (CurrentUserId is not int userId) return Unauthorized();
            try
            {
                var isFavorite = await _favoriteService.ToggleAsync(userId, productId);
                if (isFavorite == null) return NotFound();
                return Ok(new FavoriteToggleResponse { IsFavorite = isFavorite.Value });
            }
            catch (Exception ex)
            {
                // zwracamy prawdziwy powód błędu, żeby strona mogła go pokazać
                return StatusCode(500, new { message = ex.GetBaseException().Message });
            }
        }
    }
}
