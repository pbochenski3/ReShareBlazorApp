using ReShareBlazorApp.Shared.Enums;

namespace ReShareBlazorApp.Api.Date
{
    public class Payment
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public int? ProductId { get; set; }
        public Product? Product { get; set; }

        public string ProductName { get; set; } = string.Empty;

        public long AmountInGrosze { get; set; }
        public string Currency { get; set; } = "pln";

        public string? StripeSessionId { get; set; }
        public string? StripeIdempotencyKey { get; set; }
        public string? StripePaymentIntentId { get; set; }

        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? PaidAt { get; set; }
    }
}