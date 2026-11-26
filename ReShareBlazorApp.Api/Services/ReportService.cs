using ReShareBlazorApp.Api.Date;
using ReShareBlazorApp.Api.Interfaces.Repositories;
using ReShareBlazorApp.Api.Interfaces.Services;
using ReShareBlazorApp.Shared.DTOs;
using ReShareBlazorApp.Shared.Enums;

namespace ReShareBlazorApp.Api.Services
{
    public class ReportService : IReportService
    {
        private const string ImageFolder = "products";

        private readonly IReportRepository _reportRepository;
        private readonly IImageStorage _imageStorage;
        private readonly INotificationService _notificationService;

        public ReportService(
            IReportRepository reportRepository,
            IImageStorage imageStorage,
            INotificationService notificationService)
        {
            _reportRepository = reportRepository;
            _imageStorage = imageStorage;
            _notificationService = notificationService;
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
                    CreatedAt = r.CreatedAt,
                    AdminResponse = r.AdminResponse,
                    AdminRespondedAt = r.AdminRespondedAt,
                    AdminRespondedBy = r.AdminRespondedBy,
                    Status = r.Status
                })
                .ToList();
            return reportsDtoList;
        }
        public async Task<bool> ReplyToReportAsync(int reportId, string responseText, string adminUserName)
        {
            var report = await _reportRepository.GetReportByIdAsync(reportId);
            if (report == null)
                return false;

            report.AdminResponse = responseText.Trim();
            report.AdminRespondedAt = DateTime.UtcNow;
            report.AdminRespondedBy = adminUserName;
            report.Status = ReportStatus.Resolved;

            var updated = await _reportRepository.UpdateReportAsync(report);
            if (!updated)
                return false;

            try
            {
                var productName = report.Product?.Name ?? "ogłoszenia";
                await _notificationService.CreateAsync(
                    report.UserId,
                    NotificationType.ReportResponse,
                    $"Odpowiedź na zgłoszenie: {productName}",
                    $"Administrator odpowiedział na Twoje zgłoszenie: \"{responseText.Trim()}\"",
                    report.ProductId,
                    adminUserName);
            }
            catch
            {
                // Błąd powiadomienia nie unieważnia zapisu odpowiedzi
            }

            return true;
        }
        public async Task<bool> DeleteReportAsync(int reportId)
        {
            return await _reportRepository.DeleteReportAsync(reportId);
        }
        public async Task<bool> DeleteReportedProductAsync(int reportId)
        {
            var fileNames = await _reportRepository.DeleteReportedProductAsync(reportId);
            if (fileNames == null)
                return false;

            foreach (var fileName in fileNames)
            {
                _imageStorage.Delete(fileName, ImageFolder);
            }
            return true;
        }
    }
}
