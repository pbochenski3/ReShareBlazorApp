using ReShareBlazorApp.Api.Date;

namespace ReShareBlazorApp.Api.Interfaces.Repositories
{
    public interface IReportRepository
    {
        Task<bool> ProductExistsAsync(int productId);
        Task<bool> AddReportAsync(Report report);
        Task<List<Report>> GetReportsAsync();
        Task<bool> DeleteReportAsync(int reportId);
        Task<bool> DeleteReportedProductAsync(int reportId);
    }
}
