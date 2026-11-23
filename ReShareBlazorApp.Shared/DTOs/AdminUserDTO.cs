namespace ReShareBlazorApp.Shared.DTOs;

public class AdminUserDTO
{
    public int Id { get; set; }
    public string UserName { get; set; } = "";
    public string Email { get; set; } = "";
    public string? PhoneNumber { get; set; }
    public string? City { get; set; }
    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTimeOffset? LockoutEnd { get; set; }
    public string? WarningMessage { get; set; }

    public bool IsTemporarilyBlocked => LockoutEnd.HasValue && LockoutEnd.Value > DateTimeOffset.UtcNow;
    public bool IsBlocked => !IsActive || IsTemporarilyBlocked;
    public bool HasWarning => !string.IsNullOrWhiteSpace(WarningMessage);
}
