namespace ReShareBlazorApp.Shared.DTOs;

public class TemporaryBlockRequest
{
    public int Hours { get; set; }
    public DateTimeOffset? BlockedUntil { get; set; }
}
