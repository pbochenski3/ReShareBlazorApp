using Stripe;
using Stripe.Checkout;

namespace ReShareBlazorApp.Api.Services
{
    public class StripeCheckoutService
    {
        private readonly StripeClient _stripeClient;

        public StripeCheckoutService(StripeClient stripeClient)
        {
            _stripeClient = stripeClient;
        }

        public async Task<Session> CreateCheckoutSessionAsync(
            int paymentId,
            long amountInGrosze,
            string description,
            string successUrl,
            string cancelUrl,
            string idempotencyKey)
        {
            var options = new SessionCreateOptions
            {
                Mode = "payment",
                Locale = "pl",
                BillingAddressCollection = "required",

                ClientReferenceId = paymentId.ToString(),

                Metadata = new Dictionary<string, string>
                {
                    { "PaymentId", paymentId.ToString() }
                },

                LineItems =
                [
                    new SessionLineItemOptions
                    {
                        Quantity = 1,
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = "pln",
                            UnitAmount = amountInGrosze,
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = description
                            }
                        }
                    }
                ],

                SuccessUrl = successUrl,
                CancelUrl = cancelUrl
            };

            var service = new SessionService(_stripeClient);

            var requestOptions = new RequestOptions
            {
                IdempotencyKey = idempotencyKey
            };

            var session = await service.CreateAsync(
                options,
                requestOptions);

            return session;
        }
    }
}