using ReShareBlazorApp.Api.Date;

namespace ReShareBlazorApp.Api.Interfaces.Repositories
{
    public interface IReportRepository
    {
        Task<bool> ProductExistsAsync(int productId);
        Task<bool> AddReportAsync(Report report);
        Task<List<Report>> GetReportsAsync();
        Task<bool> DeleteReportAsync(int reportId);
        // Zwraca nazwy plików zdjęć usuniętego ogłoszenia albo null, gdy się nie udało.
        Task<List<string>?> DeleteReportedProductAsync(int reportId);
        Task<Report?> GetReportByIdAsync(int reportId);
        Task<bool> UpdateReportAsync(Report report);
    }
}
