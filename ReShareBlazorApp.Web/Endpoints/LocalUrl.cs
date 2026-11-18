namespace ReShareBlazorApp.Web.Endpoints
{
    public static class LocalUrl
    {
        // Przepuszcza tylko adresy wewnątrz naszej strony (np. "/odkrywaj?sort=x"),
        // żeby nikt nie podstawił linku na obcą stronę (open redirect).
        public static string Safe(string? url, string fallback = "/")
        {
            if (string.IsNullOrWhiteSpace(url)) return fallback;
            if (!url.StartsWith('/')) return fallback;
            if (url.StartsWith("//") || url.StartsWith("/\\")) return fallback;
            return url;
        }
    }
}
