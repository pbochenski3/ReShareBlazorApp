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

            var productExists = await _context.Products.AnyAsync(p => p.Id == request.ProductId);
            if (!productExists)
            {
                return NotFound("Nie znaleziono ogłoszenia.");
            }

            var authorName = !string.IsNullOrWhiteSpace(User.Identity?.Name)
                ? User.Identity.Name.Trim()
                : (string.IsNullOrWhiteSpace(request.AuthorName) ? "Anonim" : request.AuthorName.Trim());

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

            return Ok(new { message = "Opinia została dodana pomyślnie." });
        }
    }
}