using ReShareBlazorApp.Shared.Enums;

namespace ReShareBlazorApp.Shared.DTOs;

public class OrderDTO
{
    public int Id { get; set; }
    public int? ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int BuyerId { get; set; }
    public string BuyerName { get; set; } = string.Empty;
    public int SellerId { get; set; }
    public string SellerName { get; set; } = string.Empty;
    public long AmountInGrosze { get; set; }
    public string Currency { get; set; } = "pln";
    public PaymentStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public bool IsClosed { get; set; }
}