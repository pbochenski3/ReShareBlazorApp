using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReShareBlazorApp.Api.Date;
using ReShareBlazorApp.Api.Interfaces.Services;
using ReShareBlazorApp.Shared;
using ReShareBlazorApp.Shared.DTOs;
using System.Security.Claims;

namespace ReShareBlazorApp.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReviewController : ControllerBase
    {
        private readonly ReshareDbContext _context;

        private readonly IActivityLogger _activityLogger;

        public ReviewController(ReshareDbContext context, IActivityLogger activityLogger)
        {
            _context = context;
            _activityLogger = activityLogger;
        }

        [HttpGet("product/{productId:int}")]
        public async Task<ActionResult<IEnumerable<ReviewDTO>>> GetReviewsForProduct(int productId)
        {
            var reviews = await _context.Reviews
                .Where(r => r.ProductId == productId && !r.IsHidden)
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

        [HttpGet("user/{username}")]
        public async Task<ActionResult<IEnumerable<ReviewDTO>>> GetReviewsForUser(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return Ok(new List<ReviewDTO>());
            }

            var normalized = username.Trim();

            // Szukamy użytkownika o podanym loginie, aby pobrać opinie z jego ogłoszeń
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserName == normalized);

            if (user == null)
            {
                return Ok(new List<ReviewDTO>());
            }

            var reviews = await (from r in _context.Reviews
                                 join p in _context.Products on r.ProductId equals p.Id
                                 where p.UserId == user.Id
                                 orderby r.CreatedAt descending
                                 select new ReviewDTO
                                 {
                                     Id = r.Id,
                                     ProductId = r.ProductId,
                                     ProductName = p.Name,
                                     AuthorName = r.AuthorName,
                                     Rating = r.Rating,
                                     Comment = r.Comment,
                                     CreatedAt = r.CreatedAt
                                 }).ToListAsync();

            return Ok(reviews);
        }

        [HttpPost]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> AddReview([FromBody] CreateReviewRequest request)
        {
            if (request.ProductId <= 0 || request.Rating < 1 || request.Rating > 5)
            {
                return BadRequest("Nieprawidłowe dane opinii.");
            }

            if (string.IsNullOrWhiteSpace(request.Comment))
            {
                return BadRequest("Treść opinii jest wymagana.");
            }

            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdValue, out var userId))
                return Unauthorized();

            var product = await _context.Products
                .Where(p => p.Id == request.ProductId)
                .Select(p => new { p.UserId })
                .FirstOrDefaultAsync();

            if (product == null)
                return NotFound("Nie znaleziono ogłoszenia.");

            if (product.UserId == userId)
                return BadRequest("Nie możesz dodać opinii do własnego ogłoszenia.");

            var authorName = User.FindFirstValue(ClaimTypes.Name)
                ?? User.Identity?.Name
                ?? "Użytkownik";

            var review = new Review
            {
                ProductId = request.ProductId,
                AuthorName = authorName,
                Rating = request.Rating,
                Comment = request.Comment.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            var description = $"Dodano opinię ({review.Rating}/5) do ogłoszenia (id: {review.ProductId})";
            if (User.Identity?.IsAuthenticated == true)
                await _activityLogger.LogAsync(User, ActivityActions.ReviewAdded, description, "Product", review.ProductId);
            else
                await _activityLogger.LogAsync((int?)null, review.AuthorName, ActivityActions.ReviewAdded, description, "Product", review.ProductId);

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