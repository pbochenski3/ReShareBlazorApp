using System.Security.Claims;

namespace ReShareBlazorApp.Api.Interfaces.Services
{
    public interface IActivityLogger
    {
        // Użytkownik odczytywany z tokenu (NameIdentifier + Name).
        Task LogAsync(ClaimsPrincipal? principal, string action, string description,
            string? targetType = null, int? targetId = null);

        Task LogAsync(int? userId, string? userName, string action, string description,
            string? targetType = null, int? targetId = null);
    }
}
