using ReShareBlazorApp.Api.Date.Enums;

namespace ReShareBlazorApp.Shared.DTOs
{
    public class CompletedStatsDTO
    {
        // Ile rzeczy / usług zostało sprzedanych, oddanych lub wynajętych (łącznie)
        public int TotalCompleted { get; set; }

        // Kategoria, w której zakończyło się najwięcej ogłoszeń (domyślnie Bilety)
        public ProductCategory TopCategory { get; set; } = ProductCategory.Tickets;

        // Ile zakończonych ogłoszeń ma ta kategoria
        public int TopCategoryCount { get; set; }
    }
}
