namespace ReShareBlazorApp.Shared.DTOs
{
    public class ActivityLogDTO
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? TargetType { get; set; }
        public int? TargetId { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ActivityLogPageDTO
    {
        public List<ActivityLogDTO> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }
}
