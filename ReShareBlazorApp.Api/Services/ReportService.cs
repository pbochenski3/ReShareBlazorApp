using ReShareBlazorApp.Api.Date;
using ReShareBlazorApp.Api.Interfaces.Repositories;
using ReShareBlazorApp.Api.Interfaces.Services;
using ReShareBlazorApp.Shared.DTOs;
using ReShareBlazorApp.Shared.Enums;

namespace ReShareBlazorApp.Api.Services
{
    public class ReportService : IReportService
    {
        private readonly IReportRepository _reportRepository;
        public ReportService(IReportRepository reportRepository)
        {
            _reportRepository = reportRepository;
        }
        public async Task<bool> AddReportAsync(ReportDTO report)
        {
            if (!Enum.IsDefined(typeof(ReportCategory), report.Category))
                return false;

            var description = report.Description.Trim();
            if (description.Length < 10 || description.Length > 1000)
                return false;

            var productExists = await _reportRepository.ProductExistsAsync(report.ProductId);
            if (!productExists)
                return false;

            var reportModel = new Report
            {
                ProductId = report.ProductId,
                UserId = report.UserId,
                Category = report.Category,
                Description = description,
                CreatedAt = DateTime.Now
            };
            return await _reportRepository.AddReportAsync(reportModel);
        }
        public async Task<List<ReportDTO>> GetReportsAsync()
        {
            var reports = await _reportRepository.GetReportsAsync();
            var reportsDtoList = reports
                .Select(r => new ReportDTO
                {
                    ReportId = r.Id,
                    ProductId = r.ProductId,
                    ProductName = r.Product.Name,
                    ProductAddedBy = r.Product.user?.UserName ?? "",
                    UserId = r.UserId,
                    ReportedBy = r.User?.UserName ?? "",
                    Category = r.Category,
                    Description = r.Description,
                    CreatedAt = r.CreatedAt
                })
                .ToList();
            return reportsDtoList;
        }
        public async Task<bool> DeleteReportAsync(int reportId)
        {
            return await _reportRepository.DeleteReportAsync(reportId);
        }
        public async Task<bool> DeleteReportedProductAsync(int reportId)
        {
            return await _reportRepository.DeleteReportedProductAsync(reportId);
        }
    }
}
