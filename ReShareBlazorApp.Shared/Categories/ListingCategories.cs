using ReShareBlazorApp.Api.Date.Enums;

namespace ReShareBlazorApp.Shared.Categories
{
    public record CategoryGroup(string Slug, string Name, string Description, ProductCategory[] Categories);

    // Jedno źródło prawdy dla kategorii widocznych na stronie.
    // Slug trafia do URL-a (?kategoria=...), Categories to wartości enuma w bazie.
    public static class ListingCategories
    {
        public static readonly IReadOnlyList<CategoryGroup> All = new List<CategoryGroup>
        {
            new("bilety-i-rezerwacje", "Bilety i rezerwacje", "Koncerty, mecze, vouchery, bilety, spa",
                new[] { ProductCategory.Tickets, ProductCategory.Reservations }),
            new("wyjazdy-i-podroze", "Wyjazdy i podróże", "Hotele, wakacje, wycieczki, noclegi",
                new[] { ProductCategory.Trips }),
            new("transport-i-miejsca", "Transport i miejsca", "Przejazdy, samochody, busy, przewóz",
                new[] { ProductCategory.Transports }),
            new("uslugi-i-wolne-terminy", "Usługi i wolne terminy", "Fachowcy, fotografia, opieka, instruktorzy",
                new[] { ProductCategory.Services }),
        };

        public static CategoryGroup? FindBySlug(string? slug) =>
            string.IsNullOrWhiteSpace(slug)
                ? null
                : All.FirstOrDefault(c => c.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase));

        public static string NameOf(ProductCategory category) =>
            All.FirstOrDefault(c => c.Categories.Contains(category))?.Name ?? category.ToString();
    }
}
