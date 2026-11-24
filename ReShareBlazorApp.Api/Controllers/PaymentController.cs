using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReShareBlazorApp.Api.Date;
using ReShareBlazorApp.Api.Services;
using System.Security.Claims;

namespace ReShareBlazorApp.Api.Controllers
{
    [Route("api/payments")]
    [Authorize]

    public class PaymentController : ControllerBase
    {
        private readonly ReshareDbContext _context;
        private readonly StripeCheckoutService _stripe;
        private readonly IConfiguration _configuration;
        public PaymentController(
             ReshareDbContext context,
             StripeCheckoutService stripe,
             IConfiguration configuration)
        {
            _context = context;
            _stripe = stripe;
            _configuration = configuration;
        }
        [HttpPost("checkout/{productId:int}")]
        public async Task<IActionResult> CreateCheckout(int productId)
        {
            Console.WriteLine(">>> PAYMENT CONTROLLER - CreateCheckout wywołany");
            Console.WriteLine($">>> ProductId: {productId}");
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out var userId))
                return Unauthorized();
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == productId);
            if (product == null)
                return NotFound("Produkt nie istnieje.");
            if (product.UserId == userId)
                return BadRequest("Nie możesz kupić własnego produktu.");
            if (product.Price <= 0)
                return BadRequest("Nieprawidłowa cena produktu.");
            var amountInGrosze = decimal.ToInt64(
                decimal.Round(
                    product.Price * 100,
                    0,
                    MidpointRounding.AwayFromZero));
            var payment = new Payment
            {
                UserId = userId,
                ProductId = product.Id,
                ProductName = product.Name,
                AmountInGrosze = amountInGrosze,
                Currency = "pln",
                Status = ReShareBlazorApp.Shared.Enums.PaymentStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };
            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();
            try
            {
                var frontendUrl = _configuration["Frontend:BaseUrl"];
                if(string.IsNullOrWhiteSpace(frontendUrl))
                    throw new InvalidOperationException(
                      "Frontend:BaseUrl nie jest skonfigurowany.");
                var sesion = await _stripe.CreateCheckoutSessionAsync(
                    payment.Id,
                    amountInGrosze,
                    product.Name,
                    $"{frontendUrl}/payment/success",
                    $"{frontendUrl}/payment/cancel");
                payment.StripeSessionId = sesion.Id;
                await _context.SaveChangesAsync();
                return Ok(new
                {
                    checkoutUrl = sesion.Url
                });
            }
            catch
            {
                payment.Status = Shared.Enums.PaymentStatus.Failed;
                await _context.SaveChangesAsync();
                throw;
            }

        }
    }
}
