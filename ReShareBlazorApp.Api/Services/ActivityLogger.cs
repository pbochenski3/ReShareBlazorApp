using ReShareBlazorApp.Api.Date;
using ReShareBlazorApp.Api.Interfaces.Services;
using System.Security.Claims;

namespace ReShareBlazorApp.Api.Services
{
    public class ActivityLogger : IActivityLogger
    {
        private readonly ReshareDbContext _context;
        private readonly ILogger<ActivityLogger> _logger;

        public ActivityLogger(ReshareDbContext context, ILogger<ActivityLogger> logger)
        {
            _context = context;
            _logger = logger;
        }

        public Task LogAsync(ClaimsPrincipal? principal, string action, string description,
            string? targetType = null, int? targetId = null)
        {
            int? userId = int.TryParse(principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
                ? id : null;
            var userName = principal?.FindFirstValue(ClaimTypes.Name);
            return LogAsync(userId, userName, action, description, targetType, targetId);
        }

        public async Task LogAsync(int? userId, string? userName, string action, string description,
            string? targetType = null, int? targetId = null)
        {
            var entry = new ActivityLog
            {
                UserId = userId,
                UserName = Truncate(string.IsNullOrWhiteSpace(userName) ? "(nieznany)" : userName, 256),
                Action = Truncate(action, 50),
                Description = Truncate(description, 500),
                TargetType = targetType == null ? null : Truncate(targetType, 50),
                TargetId = targetId,
                CreatedAt = DateTime.UtcNow
            };
            try
            {
                _context.ActivityLogs.Add(entry);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Błąd logowania nie może zepsuć właściwej akcji użytkownika.
                _context.Entry(entry).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                _logger.LogError(ex, "[ActivityLogger] Nie udało się zapisać logu: {Action}", action);
            }
        }

        private static string Truncate(string value, int max) =>
            value.Length <= max ? value : value[..max];
    }
}
