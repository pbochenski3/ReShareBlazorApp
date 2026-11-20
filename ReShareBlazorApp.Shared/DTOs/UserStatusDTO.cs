namespace ReShareBlazorApp.Shared.DTOs;

public class UserStatusDTO
{
    public int Id { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset? LockoutEnd { get; set; }
    public string? WarningMessage { get; set; }

    public bool IsTemporarilyBlocked => LockoutEnd.HasValue && LockoutEnd.Value > DateTimeOffset.UtcNow;
    public bool IsBlocked => !IsActive || IsTemporarilyBlocked;
    public bool HasWarning => !string.IsNullOrWhiteSpace(WarningMessage);
}
