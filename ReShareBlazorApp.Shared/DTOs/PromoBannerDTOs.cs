namespace ReShareBlazorApp.Shared.DTOs
{
	public class PromoBannerDTO
	{
		public int Id { get; set; }
		public string Message { get; set; } = string.Empty;
		public string? BadgeText { get; set; }
		public string? LinkUrl { get; set; }
		public string? LinkText { get; set; }
		public DateTime CreatedAt { get; set; }
		public DateTime ExpiresAt { get; set; }
		public bool IsActive { get; set; }
	}

	public class CreatePromoBannerRequest
	{
		public string Message { get; set; } = string.Empty;
		public string? BadgeText { get; set; } = "PROMOCJA -30%";
		public string? LinkUrl { get; set; }
		public string? LinkText { get; set; }
		public int DurationHours { get; set; } = 24;
	}
}