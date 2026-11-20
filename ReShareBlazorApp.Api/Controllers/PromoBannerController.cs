using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReShareBlazorApp.Api.Date;
using ReShareBlazorApp.Shared.DTOs;

namespace ReShareBlazorApp.Api.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class PromoBannerController : ControllerBase
	{
		private readonly ReshareDbContext _context;

		public PromoBannerController(ReshareDbContext context)
		{
			_context = context;
		}

		// GET: api/promobanner/active
		[HttpGet("active")]
		[AllowAnonymous]
		public async Task<IActionResult> GetActiveBanner()
		{
			var now = DateTime.UtcNow;

			var banner = await _context.PromoBanners
				.Where(b => b.IsActive && b.ExpiresAt > now)
				.OrderByDescending(b => b.CreatedAt)
				.FirstOrDefaultAsync();

			if (banner == null)
				return Ok((PromoBannerDTO?)null);

			return Ok(new PromoBannerDTO
			{
				Id = banner.Id,
				Message = banner.Message,
				BadgeText = banner.BadgeText,
				LinkUrl = banner.LinkUrl,
				LinkText = banner.LinkText,
				CreatedAt = banner.CreatedAt,
				ExpiresAt = banner.ExpiresAt,
				IsActive = banner.IsActive
			});
		}

		// GET: api/promobanner/current
		[HttpGet("current")]
		[Authorize(Roles = "Admin")]
		public async Task<IActionResult> GetCurrentForAdmin()
		{
			var banner = await _context.PromoBanners
				.OrderByDescending(b => b.CreatedAt)
				.FirstOrDefaultAsync();

			if (banner == null)
				return Ok((PromoBannerDTO?)null);

			return Ok(new PromoBannerDTO
			{
				Id = banner.Id,
				Message = banner.Message,
				BadgeText = banner.BadgeText,
				LinkUrl = banner.LinkUrl,
				LinkText = banner.LinkText,
				CreatedAt = banner.CreatedAt,
				ExpiresAt = banner.ExpiresAt,
				IsActive = banner.IsActive && banner.ExpiresAt > DateTime.UtcNow
			});
		}

		// POST: api/promobanner
		[HttpPost]
		[Authorize(Roles = "Admin")]
		public async Task<IActionResult> SetPromoBanner([FromBody] CreatePromoBannerRequest request)
		{
			if (string.IsNullOrWhiteSpace(request.Message))
				return BadRequest("Treść paska promocyjnego jest wymagana.");

			if (request.DurationHours <= 0 || request.DurationHours > 720)
				return BadRequest("Czas trwania musi wynosić od 1 do 720 godzin.");

			var activeBanners = await _context.PromoBanners
				.Where(b => b.IsActive)
				.ToListAsync();

			foreach (var old in activeBanners)
			{
				old.IsActive = false;
			}

			var now = DateTime.UtcNow;
			var newBanner = new PromoBanner
			{
				Message = request.Message.Trim(),
				BadgeText = string.IsNullOrWhiteSpace(request.BadgeText) ? null : request.BadgeText.Trim(),
				LinkUrl = string.IsNullOrWhiteSpace(request.LinkUrl) ? null : request.LinkUrl.Trim(),
				LinkText = string.IsNullOrWhiteSpace(request.LinkText) ? null : request.LinkText.Trim(),
				CreatedAt = now,
				ExpiresAt = now.AddHours(request.DurationHours),
				IsActive = true
			};

			_context.PromoBanners.Add(newBanner);
			await _context.SaveChangesAsync();

			return Ok(new PromoBannerDTO
			{
				Id = newBanner.Id,
				Message = newBanner.Message,
				BadgeText = newBanner.BadgeText,
				LinkUrl = newBanner.LinkUrl,
				LinkText = newBanner.LinkText,
				CreatedAt = newBanner.CreatedAt,
				ExpiresAt = newBanner.ExpiresAt,
				IsActive = newBanner.IsActive
			});
		}

		// POST: api/promobanner/disable
		[HttpPost("disable")]
		[Authorize(Roles = "Admin")]
		public async Task<IActionResult> DisableBanner()
		{
			var activeBanners = await _context.PromoBanners
				.Where(b => b.IsActive)
				.ToListAsync();

			foreach (var b in activeBanners)
			{
				b.IsActive = false;
			}

			await _context.SaveChangesAsync();
			return Ok(new { message = "Wyłączono pasek promocyjny." });
		}
	}
}