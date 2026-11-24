
namespace ReShareBlazorApp.Api.Date;

public class RecentlyViewedProduct
{
	public int Id { get; set; }

	public int UserId { get; set; }
	public User User { get; set; } = null!;

	public int ProductId { get; set; }
	public Product Product { get; set; } = null!;

	public DateTime ViewedAt { get; set; }
}
