namespace ReShareBlazorApp.Web.Endpoints
{
    public static class AvatarEndpoints
    {
        // Zdjęcia profilowe pobierane z API i serwowane z tego samego adresu co strona,
        // dzięki czemu przeglądarka nie blokuje ich przez certyfikat/inny port API.
        public static void MapAvatarEndpoints(this WebApplication app)
        {
            app.MapGet("/avatars/{fileName}", async (
                string fileName,
                HttpContext context,
                IHttpClientFactory httpClientFactory) =>
            {
                var safeName = Path.GetFileName(fileName);
                var client = httpClientFactory.CreateClient("API");
                var response = await client.GetAsync(
                    $"avatars/{Uri.EscapeDataString(safeName)}",
                    HttpCompletionOption.ResponseHeadersRead);
                if (!response.IsSuccessStatusCode)
                {
                    response.Dispose();
                    return Results.NotFound();
                }
                // Nazwy plików są unikalne, więc można bezpiecznie cache'ować.
                context.Response.Headers.CacheControl = "public,max-age=86400";
                context.Response.RegisterForDispose(response);
                var stream = await response.Content.ReadAsStreamAsync();
                var contentType = response.Content.Headers.ContentType?.MediaType
                    ?? "application/octet-stream";
                return Results.Stream(stream, contentType);
            });
        }
    }
}
