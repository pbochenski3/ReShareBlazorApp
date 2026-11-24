
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

            if (stripeEvent.Type == "checkout.session.completed" ||
                stripeEvent.Type == "checkout.session.async_payment_succeeded")
            {
                var session = stripeEvent.Data.Object as Session;

                if (session == null)
                    return BadRequest();

                if (session.PaymentStatus != "paid")
                    return Ok();

                var payment = await _context.Payments
                    .FirstOrDefaultAsync(p =>
                        p.StripeSessionId == session.Id);

                if (payment == null)
                    return NotFound("Payment not found.");

                if (payment.Status == PaymentStatus.Paid)
                    return Ok();

                payment.Status = PaymentStatus.Paid;
                payment.PaidAt = DateTime.UtcNow;
                payment.StripePaymentIntentId = session.PaymentIntentId;

                await _context.SaveChangesAsync();
            }

            return Ok();
        }
    }
}
