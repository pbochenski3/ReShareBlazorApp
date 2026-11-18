using Microsoft.AspNetCore.Mvc;
using ReShareBlazorApp.Web.ApiServices;

namespace ReShareBlazorApp.Web.Endpoints
{
    public static class FavoriteEndpoints
    {
        public static void MapFavoriteEndpoints(this WebApplication app)
        {
            // Gwiazdka to formularz POST. Gdy JavaScript działa, strona prosi o JSON i nie przeładowuje się;
            // bez JavaScriptu endpoint przekierowuje z powrotem na stronę.
            app.MapPost("/favorites/toggle", async Task<IResult> (
                [FromForm] int productId,
                [FromForm] string? returnUrl,
                HttpContext context,
                FavoriteApiService favoriteApi) =>
            {
                var back = LocalUrl.Safe(returnUrl);            // dokąd wrócić po kliknięciu
                var loginUrl = $"/login?returnUrl={Uri.EscapeDataString(back)}";
                var wantsJson = context.Request.Headers.Accept.ToString().Contains("application/json");
                var token = context.User.FindFirst("access_token")?.Value;

                if (string.IsNullOrEmpty(token))
                {
                    // niezalogowany -> najpierw logowanie, potem powrót na tę samą stronę
                    return wantsJson
                        ? Results.Json(new { needsLogin = true, loginUrl }, statusCode: 401)
                        : Results.LocalRedirect(loginUrl);
                }

                var result = await favoriteApi.ToggleAsync(productId, token);
                if (result.Unauthorized)
                {
                    return wantsJson
                        ? Results.Json(new { needsLogin = true, loginUrl }, statusCode: 401)
                        : Results.LocalRedirect(loginUrl);
                }
                if (wantsJson)
                {
                    return result.IsFavorite is bool isFavorite
                        ? Results.Json(new { isFavorite })
                        : Results.Json(new { error = result.Error ?? "Nie udało się zapisać ulubionych." }, statusCode: 502);
                }
                return Results.LocalRedirect($"{back}#listing-{productId}"); // wracamy do tej samej karty
            });
        }
    }
}
