using ReShareBlazorApp.Shared.DTOs;

namespace ReShareBlazorApp.Api.Interfaces.Services
{
    public interface IReportService
    {
        Task<bool> AddReportAsync(ReportDTO report);
        Task<List<ReportDTO>> GetReportsAsync();
        Task<bool> DeleteReportAsync(int reportId);
        Task<bool> DeleteReportedProductAsync(int reportId);
    }
}
