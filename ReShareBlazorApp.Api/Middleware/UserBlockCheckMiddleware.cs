using Microsoft.EntityFrameworkCore;
using ReShareBlazorApp.Api.Date;
using System.Security.Claims;

namespace ReShareBlazorApp.Api.Middleware
{
    public class UserBlockCheckMiddleware
    {
        private readonly RequestDelegate _next;

        public UserBlockCheckMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, ReshareDbContext dbContext)
        {
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var path = context.Request.Path;
                // Pozwalamy na sprawdzanie własnego statusu oraz endpointy admina
                if (!path.StartsWithSegments("/api/user/my-status") && !path.StartsWithSegments("/api/admin"))
                {
                    var userIdClaim = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (int.TryParse(userIdClaim, out var userId))
                    {
                        var user = await dbContext.Users
                            .AsNoTracking()
                            .Where(u => u.Id == userId)
                            .Select(u => new { u.IsActive, u.LockoutEnd })
                            .FirstOrDefaultAsync();

                        if (user != null)
                        {
                            var isLockedOut = user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow;
                            if (!user.IsActive || isLockedOut)
                            {
                                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                                context.Response.ContentType = "application/json";
                                var msg = !user.IsActive
                                    ? "Twoje konto zostało zablokowane na stałe przez administratora."
                                    : (user.LockoutEnd.HasValue
                                        ? $"Twoje konto zostało tymczasowo zablokowane do {user.LockoutEnd.Value.ToLocalTime():dd.MM.yyyy HH:mm}."
                                        : "Twoje konto zostało zablokowane przez administratora.");

                                await context.Response.WriteAsJsonAsync(new { message = msg, isBlocked = true });
                                return;
                            }
                        }
                    }
                }
            }

            await _next(context);
        }
    }
}
