namespace ReShareBlazorApp.Api.Date
{
	public class PromoBanner
	{
		public int Id { get; set; }
		public string Message { get; set; } = string.Empty;
		public string? BadgeText { get; set; }
		public string? LinkUrl { get; set; }
		public string? LinkText { get; set; }
		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
		public DateTime ExpiresAt { get; set; }
		public bool IsActive { get; set; } = true;
	}
}