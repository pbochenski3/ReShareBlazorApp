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

            var productExists = await _context.Products.AnyAsync(p => p.Id == request.ProductId);
            if (!productExists)
            {
                return NotFound("Nie znaleziono ogłoszenia.");
            }

            var review = new Review
            {
                ProductId = request.ProductId,
                AuthorName = string.IsNullOrWhiteSpace(request.AuthorName) ? "Anonim" : request.AuthorName.Trim(),
                Rating = request.Rating,
                Comment = request.Comment.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Opinia została dodana pomyślnie." });
        }
        [HttpGet("user/{userId:int}")]
        public async Task<ActionResult<UserRatingDTO>> GetUserRating(int userId)
        {
            var reviews = await _context.Reviews
                .Where(r => _context.Products.Any(p =>
                    p.Id == r.ProductId &&
                    p.UserId == userId &&
                    !p.isDeleted))
                .Select(r => r.Rating)
                .ToListAsync();

            return Ok(new UserRatingDTO
            {
                AverageRating = reviews.Any() ? reviews.Average() : 0,
                TotalReviews = reviews.Count
            });
        }
    }
}