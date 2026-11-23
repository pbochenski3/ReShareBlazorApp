namespace ReShareBlazorApp.Api.Date
{
    public class UserBadge
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int BadgeId { get; set; }
        public bool IsEarned { get; set; } = false;
        public DateTime EarnedAt { get; set; } = DateTime.Now;
        public Badge Badge { get; set; } = null!;
    }
}
