using ReShareBlazorApp.Shared.DTOs;

namespace ReShareBlazorApp.Web;

public static class LockdownExtensions
{
    private static bool _locked;
    private static DateTime _checkedAt = DateTime.MinValue;

    public static IApplicationBuilder UseLockdown(this IApplicationBuilder app) =>
        app.Use(async (ctx, next) =>
        {
            var path = ctx.Request.Path.Value ?? "";
            var open = new[] { "/login", "/admin", "/_blazor", "/_framework", "/_content", "/css", "/js", "/favicon" };

            if (open.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)) || ctx.User.IsInRole("Admin"))
            {
                await next();
                return;
            }

            // stan z API odświeżany co 5 s
            if (DateTime.UtcNow - _checkedAt > TimeSpan.FromSeconds(5))
            {
                try
                {
                    var client = ctx.RequestServices.GetRequiredService<IHttpClientFactory>().CreateClient("API");
                    _locked = (await client.GetFromJsonAsync<LockdownDTO>("api/lockdown"))?.IsLocked ?? false;
                }
                catch { /* API nie odpowiada – zostaje poprzedni stan */ }
                _checkedAt = DateTime.UtcNow;
            }

            if (!_locked)
            {
                await next();
                return;
            }

            ctx.Response.StatusCode = 503;
            ctx.Response.ContentType = "text/html; charset=utf-8";
            await ctx.Response.WriteAsync(
                "<!doctype html><meta charset=utf-8><meta name=viewport content='width=device-width,initial-scale=1'>" +
                "<body style='font-family:sans-serif;text-align:center;margin-top:20vh'>" +
                "<h1>Przerwa techniczna</h1><p>Wróć za chwilę.</p></body>");
        });
}