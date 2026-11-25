using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReShareBlazorApp.Api.Date;
using ReShareBlazorApp.Api.Interfaces.Services;
using ReShareBlazorApp.Api.Services;
using ReShareBlazorApp.Shared.DTOs;
using ReShareBlazorApp.Shared.Enums;
using System.Security.Claims;
using Stripe;
using Stripe.Checkout;

namespace ReShareBlazorApp.Api.Controllers
{
    [Route("api/payments")]
    [Authorize]

    public class PaymentController : ControllerBase
    {
        private readonly ReshareDbContext _context;
        private readonly StripeCheckoutService _stripe;
        private readonly StripeClient _stripeClient;
        private readonly IConfiguration _configuration;
        private readonly IProductService _productService;
        public PaymentController(
             ReshareDbContext context,
             StripeCheckoutService stripe,
             StripeClient stripeClient,
             IConfiguration configuration,
             IProductService productService)
        {
            _context = context;
            _stripe = stripe;
            _stripeClient = stripeClient;
            _configuration = configuration;
            _productService = productService;
        }
        [HttpPost("checkout/{productId:int}")]
        public async Task<IActionResult> CreateCheckout(int productId)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out var userId))
                return Unauthorized();
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == productId);
            if (product == null)
                return NotFound("Produkt nie istnieje.");
            if (product.UserId == userId)
                return BadRequest("Nie możesz kupić własnego produktu.");
            if (product.Type != ReShareBlazorApp.Shared.Enums.ProductType.Sell)
                return BadRequest("Ten typ ogłoszenia nie jest przeznaczony do zakupu.");

            if (product.Price < 2m)
                return BadRequest("Minimalna cena sprzedaży wynosi 2 zł.");

            var alreadyOrdered = await _context.Payments.AnyAsync(p =>
                p.ProductId == product.Id &&
                (p.Status == PaymentStatus.Pending ||
                 p.Status == PaymentStatus.Paid ||
                 p.Status == PaymentStatus.AwaitingShipment ||
                 p.Status == PaymentStatus.Shipped ||
                 p.Status == PaymentStatus.Delivered ||
                 p.Status == PaymentStatus.NotDelivered));

            if (alreadyOrdered)
                return Conflict("Ten produkt został już kupiony.");
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
                StripeIdempotencyKey = $"payment-{Guid.NewGuid():N}",
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
                    $"{frontendUrl}/payment/success?session_id={{CHECKOUT_SESSION_ID}}",
                    $"{frontendUrl}/payment/cancel?product_id={product.Id}",
                    payment.StripeIdempotencyKey!);
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

        [AllowAnonymous]
        [HttpGet("product/{productId:int}/availability")]
        public async Task<IActionResult> GetProductAvailability(int productId)
        {
            var unavailable = await _context.Payments.AnyAsync(p =>
                p.ProductId == productId &&
                (p.Status == PaymentStatus.Pending ||
                 p.Status == PaymentStatus.Paid ||
                 p.Status == PaymentStatus.AwaitingShipment ||
                 p.Status == PaymentStatus.Shipped ||
                 p.Status == PaymentStatus.Delivered ||
                 p.Status == PaymentStatus.NotDelivered));

            return Ok(new { unavailable });
        }

        [HttpPost("cancel/{productId:int}")]
        public async Task<IActionResult> CancelCheckout(int productId)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized();

            var payment = await _context.Payments
                .Where(p => p.ProductId == productId &&
                            p.UserId == userId &&
                            p.Status == PaymentStatus.Pending)
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefaultAsync();

            if (payment == null)
                return NotFound();

            payment.Status = PaymentStatus.Failed;
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpPost("confirm/{sessionId}")]
        public async Task<IActionResult> ConfirmCheckout(string sessionId)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized();

            if (string.IsNullOrWhiteSpace(sessionId))
                return BadRequest("Brak identyfikatora sesji płatności.");

            var sessionService = new SessionService(_stripeClient);
            var session = await sessionService.GetAsync(sessionId);

            if (session == null || session.PaymentStatus != "paid")
                return BadRequest("Płatność nie została potwierdzona.");

            var paymentIdText = session.ClientReferenceId;
            if (string.IsNullOrWhiteSpace(paymentIdText) && session.Metadata != null)
                session.Metadata.TryGetValue("PaymentId", out paymentIdText);

            if (!int.TryParse(paymentIdText, out var paymentId))
                return BadRequest("Sesja nie jest powiązana z zamówieniem.");

            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.Id == paymentId && p.UserId == userId);

            if (payment == null)
                return NotFound("Płatność nie istnieje.");

            if (payment.Status == PaymentStatus.Paid)
            {
                payment.Status = PaymentStatus.AwaitingShipment;
                await _context.SaveChangesAsync();
                return Ok(new { paid = true });
            }

            var anotherOrderExists = payment.ProductId.HasValue &&
                await _context.Payments.AnyAsync(p =>
                    p.Id != payment.Id &&
                    p.ProductId == payment.ProductId &&
                    (p.Status == PaymentStatus.Paid ||
                     p.Status == PaymentStatus.AwaitingShipment ||
                     p.Status == PaymentStatus.Shipped ||
                     p.Status == PaymentStatus.Delivered ||
                     p.Status == PaymentStatus.NotDelivered));

            if (anotherOrderExists)
            {
                payment.Status = PaymentStatus.Cancelled;
                await _context.SaveChangesAsync();
                return Conflict("Produkt został już kupiony.");
            }

            payment.Status = PaymentStatus.AwaitingShipment;
            payment.PaidAt = DateTime.UtcNow;
            payment.StripeSessionId = session.Id;
            payment.StripePaymentIntentId = session.PaymentIntentId;

            await _context.SaveChangesAsync();
            return Ok(new { paid = true });
        }

        [HttpGet("orders")]
        public async Task<ActionResult<List<OrderDTO>>> GetMyOrders()
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized();

            var orders = await _context.Payments
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(p => p.UserId == userId &&
                    (p.Status == PaymentStatus.AwaitingShipment ||
                     p.Status == PaymentStatus.Shipped ||
                     p.Status == PaymentStatus.Delivered ||
                     p.Status == PaymentStatus.NotDelivered))
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new OrderDTO
                {
                    Id = p.Id,
                    ProductId = p.ProductId,
                    ProductName = p.ProductName,
                    BuyerId = p.UserId,
                    BuyerName = p.User.UserName ?? "Użytkownik",
                    SellerId = p.Product != null ? p.Product.UserId : 0,
                    SellerName = p.Product != null && p.Product.user != null
                        ? p.Product.user.UserName ?? "Użytkownik"
                        : "Użytkownik",
                    AmountInGrosze = p.AmountInGrosze,
                    Currency = p.Currency,
                    Status = p.Status,
                    CreatedAt = p.CreatedAt,
                    PaidAt = p.PaidAt
                })
                .ToListAsync();

            return Ok(orders);
        }

        [HttpGet("sales")]
        public async Task<ActionResult<List<OrderDTO>>> GetMySales()
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized();

            var salesQuery = _context.Payments
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(p => p.Product != null && p.Product.UserId == userId &&
                    (p.Status == PaymentStatus.AwaitingShipment ||
                     p.Status == PaymentStatus.Shipped ||
                     p.Status == PaymentStatus.NotDelivered ||
                     (p.Status == PaymentStatus.Delivered && !p.Product.isSold)))
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new OrderDTO
                {
                    Id = p.Id,
                    ProductId = p.ProductId,
                    ProductName = p.ProductName,
                    BuyerId = p.UserId,
                    BuyerName = p.User.UserName ?? "Użytkownik",
                    SellerId = userId,
                    SellerName = p.Product!.user.UserName ?? "Użytkownik",
                    AmountInGrosze = p.AmountInGrosze,
                    Currency = p.Currency,
                    Status = p.Status,
                    CreatedAt = p.CreatedAt,
                     PaidAt = p.PaidAt,
                     IsClosed = false
                 });

            var closedSalesQuery = _context.Payments
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(p => p.Product != null && p.Product.UserId == userId &&
                            p.Product.isSold &&
                            p.Status == PaymentStatus.Delivered)
                .OrderByDescending(p => p.CreatedAt)
                .Take(10)
                .Select(p => new OrderDTO
                {
                    Id = p.Id,
                    ProductId = p.ProductId,
                    ProductName = p.ProductName,
                    BuyerId = p.UserId,
                    BuyerName = p.User.UserName ?? "Użytkownik",
                    SellerId = userId,
                    SellerName = p.Product!.user.UserName ?? "Użytkownik",
                    AmountInGrosze = p.AmountInGrosze,
                    Currency = p.Currency,
                    Status = p.Status,
                    CreatedAt = p.CreatedAt,
                    PaidAt = p.PaidAt,
                    IsClosed = true
                });

            var sales = await salesQuery.ToListAsync();
            var closedSales = await closedSalesQuery.ToListAsync();
            sales.AddRange(closedSales);
            sales = sales.OrderByDescending(s => s.CreatedAt).ToList();

            return Ok(sales);
        }

        [HttpPatch("{paymentId:int}/status")]
        public async Task<IActionResult> UpdateOrderStatus(
            int paymentId,
            [FromBody] PaymentStatus status)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized();

            var payment = await _context.Payments
                .IgnoreQueryFilters()
                .Include(p => p.Product)
                .FirstOrDefaultAsync(p => p.Id == paymentId);

            if (payment?.Product == null)
                return NotFound("Nie znaleziono zamówienia.");

            if (payment.Product.UserId != userId)
                return Forbid();

            if (!IsValidSellerTransition(payment.Status, status))
                return BadRequest("Nieprawidłowa zmiana statusu zamówienia.");

            payment.Status = status;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPatch("{paymentId:int}/buyer-status")]
        public async Task<IActionResult> UpdateBuyerStatus(
            int paymentId,
            [FromBody] PaymentStatus status)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized();

            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.Id == paymentId && p.UserId == userId);

            if (payment == null)
                return NotFound("Nie znaleziono zamówienia.");

            if (payment.Status != PaymentStatus.Shipped ||
                status != PaymentStatus.Delivered)
                return BadRequest("Nieprawidłowa zmiana statusu dostawy.");

            payment.Status = status;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpPost("{paymentId:int}/close-product")]
        public async Task<IActionResult> CloseProduct(int paymentId)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized();

            var payment = await _context.Payments
                .IgnoreQueryFilters()
                .Include(p => p.Product)
                .FirstOrDefaultAsync(p => p.Id == paymentId);

            if (payment?.Product == null)
                return NotFound("Nie znaleziono ogłoszenia.");

            if (payment.Product.UserId != userId)
                return Forbid();

            if (payment.Status != PaymentStatus.Delivered)
                return BadRequest("Ogłoszenie można zamknąć dopiero po potwierdzeniu dostawy.");

            if (!await _productService.MarkAsSoldAsync(payment.ProductId!.Value, userId))
                return NotFound("Nie udało się zamknąć ogłoszenia.");

            return NoContent();
        }

        private bool TryGetUserId(out int userId)
        {
            return int.TryParse(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                out userId);
        }

        private static bool IsValidSellerTransition(
            PaymentStatus current,
            PaymentStatus next)
        {
            return current switch
            {
                PaymentStatus.AwaitingShipment => next == PaymentStatus.Shipped,
                PaymentStatus.NotDelivered => next == PaymentStatus.Shipped,
                _ => false
            };
        }

    }
}
