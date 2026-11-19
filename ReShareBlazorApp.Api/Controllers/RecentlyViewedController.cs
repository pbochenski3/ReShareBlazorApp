
using System.Data;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReShareBlazorApp.Api.Date;
using ReShareBlazorApp.Shared.DTOs;

namespace ReShareBlazorApp.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/recently-viewed")]
public class RecentlyViewedController : ControllerBase
{
	private const int MaxItems = 5;

	private readonly ReshareDbContext _db;

	public RecentlyViewedController(ReshareDbContext db)
	{
		_db = db;
	}

	private int? GetCurrentUserId()
	{
		var value =
			User.FindFirstValue(ClaimTypes.NameIdentifier)
			?? User.FindFirstValue("sub");

		return int.TryParse(value, out var id) ? id : null;
	}

	// POST api/recently-viewed/123
	[HttpPost("{productId:int}")]
	public async Task<IActionResult> Add(
		int productId,
		CancellationToken cancellationToken)
	{
		var userId = GetCurrentUserId();

		if (userId == null)
			return Unauthorized();

		if (productId <= 0)
			return BadRequest("Nieprawidłowe ID ogłoszenia.");

		var exists = await _db.Products
			.AnyAsync(x => x.Id == productId, cancellationToken);

		if (!exists)
			return NotFound("Ogłoszenie nie istnieje.");

		// Chroni operację przed równoczesnym zapisem
		// kilku odwiedzanych ogłoszeń przez tego samego użytkownika.
		await using var transaction =
			await _db.Database.BeginTransactionAsync(
				IsolationLevel.Serializable,
				cancellationToken);

		var existing = await _db.RecentlyViewedProducts
			.FirstOrDefaultAsync(
				x => x.UserId == userId.Value
				  && x.ProductId == productId,
				cancellationToken);

		if (existing == null)
		{
			_db.RecentlyViewedProducts.Add(
				new RecentlyViewedProduct
				{
					UserId = userId.Value,
					ProductId = productId,
					ViewedAt = DateTime.UtcNow
				});
		}
		else
		{
			// Ponowne odwiedzenie przenosi produkt na początek.
			existing.ViewedAt = DateTime.UtcNow;
		}

		await _db.SaveChangesAsync(cancellationToken);

		var oldest = await _db.RecentlyViewedProducts
			.Where(x => x.UserId == userId.Value)
			.OrderByDescending(x => x.ViewedAt)
			.ThenByDescending(x => x.Id)
			.Skip(MaxItems)
			.ToListAsync(cancellationToken);

		if (oldest.Count > 0)
		{
			_db.RecentlyViewedProducts.RemoveRange(oldest);
			await _db.SaveChangesAsync(cancellationToken);
		}

		await transaction.CommitAsync(cancellationToken);

		return NoContent();
	}

	// GET api/recently-viewed
	[HttpGet]
	public async Task<ActionResult<List<ProductDTO>>> Get(
		CancellationToken cancellationToken)
	{
		var userId = GetCurrentUserId();

		if (userId == null)
			return Unauthorized();

		var products = await _db.RecentlyViewedProducts
			.AsNoTracking()
			.Where(x => x.UserId == userId.Value)
			.OrderByDescending(x => x.ViewedAt)
			.ThenByDescending(x => x.Id)
			.Take(MaxItems)
			.Select(x => new ProductDTO
			{
				ProductId = x.Product.Id,
				Name = x.Product.Name,
				Description = x.Product.Description,
				City = x.Product.City,
				Price = x.Product.Price,
				AddedAt = x.Product.AddedAt,
				ExpiresAt = x.Product.ExpiresAt,
				Type = x.Product.Type,
				Category = x.Product.Category,
				UserId = x.Product.UserId,
				AddedBy = x.Product.user.UserName ?? string.Empty
			})
			.ToListAsync(cancellationToken);

		return Ok(products);
	}
}
