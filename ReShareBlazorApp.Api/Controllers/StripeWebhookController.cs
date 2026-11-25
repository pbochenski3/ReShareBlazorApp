
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReShareBlazorApp.Api.Date;
using ReShareBlazorApp.Shared.Enums;
using Stripe;
using Stripe.Checkout;

namespace ReShareBlazorApp.Api.Controllers
{
    [ApiController]
    [AllowAnonymous]
    [Route("api/stripe/webhook")]
    public class StripeWebhookController : ControllerBase
    {
        private readonly ReshareDbContext _context;
        private readonly IConfiguration _configuration;

        public StripeWebhookController(
            ReshareDbContext context,
            IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpPost]
        public async Task<IActionResult> HandleWebhook()
        {
            var json = await new StreamReader(Request.Body)
                .ReadToEndAsync();

            var signature = Request.Headers["Stripe-Signature"].ToString();

            var webhookSecret = _configuration["Stripe:WebhookSecret"];

            if (string.IsNullOrWhiteSpace(webhookSecret))
                return StatusCode(500, "Webhook secret is not configured.");

            Event stripeEvent;

            try
            {
                stripeEvent = EventUtility.ConstructEvent(
                    json,
                    signature,
                    webhookSecret);
            }
            catch (StripeException)
            {
                return BadRequest("Invalid Stripe signature.");
            }

            if (stripeEvent.Type == "checkout.session.expired" ||
                stripeEvent.Type == "checkout.session.async_payment_failed")
            {
                var session = stripeEvent.Data.Object as Session;
                if (session == null)
                    return BadRequest();

                var failedPayment = await _context.Payments
                    .FirstOrDefaultAsync(p => p.StripeSessionId == session.Id);

                if (failedPayment != null && failedPayment.Status == PaymentStatus.Pending)
                {
                    failedPayment.Status = PaymentStatus.Failed;
                    await _context.SaveChangesAsync();
                }

                return Ok();
            }

            if (stripeEvent.Type == "checkout.session.completed" ||
                stripeEvent.Type == "checkout.session.async_payment_succeeded")
            {
                var session = stripeEvent.Data.Object as Session;

                if (session == null)
                    return BadRequest();

                if (session.PaymentStatus != "paid")
                    return Ok();

                var paymentId = session.ClientReferenceId;
                if (string.IsNullOrWhiteSpace(paymentId) && session.Metadata != null)
                    session.Metadata.TryGetValue("PaymentId", out paymentId);

                Payment? payment = null;
                if (int.TryParse(paymentId, out var parsedPaymentId))
                {
                    payment = await _context.Payments
                        .FirstOrDefaultAsync(p => p.Id == parsedPaymentId);
                }

                payment ??= await _context.Payments
                    .FirstOrDefaultAsync(p => p.StripeSessionId == session.Id);

                if (payment == null)
                    return NotFound("Payment not found.");

                if (payment.Status == PaymentStatus.AwaitingShipment ||
                    payment.Status == PaymentStatus.Shipped ||
                    payment.Status == PaymentStatus.Delivered)
                    return Ok();

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
                    return Ok();
                }

                payment.Status = PaymentStatus.AwaitingShipment;
                payment.PaidAt = DateTime.UtcNow;
                payment.StripePaymentIntentId = session.PaymentIntentId;

                await _context.SaveChangesAsync();
            }

            return Ok();
        }
    }
}
