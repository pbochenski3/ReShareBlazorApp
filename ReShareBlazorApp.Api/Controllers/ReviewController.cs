using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReShareBlazorApp.Api.Date;
using ReShareBlazorApp.Shared.DTOs;

namespace ReShareBlazorApp.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReviewController : ControllerBase
    {
        private readonly ReshareDbContext _context;

        public ReviewController(ReshareDbContext context)
        {
            _context = context;
        }

        [HttpGet("product/{productId:int}")]
        public async Task<ActionResult<IEnumerable<ReviewDTO>>> GetReviewsForProduct(int productId)
        {
            var reviews = await _context.Reviews
                .Where(r => r.ProductId == productId)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new ReviewDTO
                {
                    Id = r.Id,
                    ProductId = r.ProductId,
                    AuthorName = r.AuthorName,
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAt
                })
                .ToListAsync();

            return Ok(reviews);
        }

        [HttpPost]
        public async Task<IActionResult> AddReview([FromBody] CreateReviewRequest request)
        {
            if (request.ProductId <= 0 || request.Rating < 1 || request.Rating > 5)
            {
                return BadRequest("Nieprawidłowe dane opinii.");
            }

            var product = await _context.Products.FindAsync(request.ProductId);
            if (product == null)
            {
                return NotFound("Nie znaleziono ogłoszenia.");
            }

            // Pobieramy ID zalogowanego użytkownika z kontekstu żądania API
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Jeśli encja Product posiada właściwość UserId (sprawdzamy czy to string i czy nie jest puste)
            // Możesz dostosować nazwę właściwości w bazie, jeśli w encji Product nazywa się inaczej (np. OwnerId)
            if (!string.IsNullOrEmpty(currentUserId))
            {
                // Jeśli typ product.UserId w bazie to int, a currentUserId to string, upewnij się co do konwersji,
                // lub jeśli encja Product w ogóle nie ma pola UserId, ten blok zostanie pominięty.
            }

            // Zapisujemy nową opinię
            var review = new Review
            {
                ProductId = request.ProductId,
                AuthorName = string.IsNullOrWhiteSpace(request.AuthorName) ? "Anonim" : request.AuthorName,
                Rating = request.Rating,
                Comment = request.Comment,
                CreatedAt = DateTime.UtcNow
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Opinia została dodana pomyślnie." });
        }
    }
}