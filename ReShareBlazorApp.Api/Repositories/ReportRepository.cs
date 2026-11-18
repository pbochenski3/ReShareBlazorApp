using Microsoft.EntityFrameworkCore;
using ReShareBlazorApp.Api.Date;
using ReShareBlazorApp.Api.Interfaces.Repositories;

namespace ReShareBlazorApp.Api.Repositories
{
    public class ReportRepository : IReportRepository
    {
        private readonly ReshareDbContext _context;
        private readonly ILogger<ReportRepository> _logger;
        public ReportRepository(ReshareDbContext context, ILogger<ReportRepository> logger)
        {
            _context = context;
            _logger = logger;
        }
        public async Task<bool> ProductExistsAsync(int productId)
        {
            try
            {
                return await _context.Products.AnyAsync(p => p.Id == productId);
            }
            catch (Exception ex)
            {
                _logger.LogError($"[ReportRepository] Error occured in ProductExistsAsync : {ex}");
                return false;
            }
        }
        public async Task<bool> AddReportAsync(Report report)
        {
            try
            {
                await _context.Reports.AddAsync(report);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"[ReportRepository] Error occured in AddReportAsync : {ex}");
                return false;
            }
        }
        public async Task<List<Report>> GetReportsAsync()
        {
            try
            {
                return await _context.Reports
                    .Include(r => r.User)
                    .Include(r => r.Product)
                        .ThenInclude(p => p.user)
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"[ReportRepository] Error occured in GetReportsAsync : {ex}");
                return new List<Report>();
            }
        }
        public async Task<bool> DeleteReportAsync(int reportId)
        {
            try
            {
                var report = await _context.Reports
                    .FirstOrDefaultAsync(r => r.Id == reportId);
                if (report == null)
                    return false;

                _context.Reports.Remove(report);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"[ReportRepository] Error occured in DeleteReportAsync : {ex}");
                return false;
            }
        }
        public async Task<bool> DeleteReportedProductAsync(int reportId)
        {
            try
            {
                var report = await _context.Reports
                    .FirstOrDefaultAsync(r => r.Id == reportId);
                if (report == null)
                    return false;

                var product = await _context.Products
                    .FirstOrDefaultAsync(p => p.Id == report.ProductId);
                if (product == null)
                    return false;

                // Usunięcie ogłoszenia usuwa kaskadowo wszystkie jego zgłoszenia.
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"[ReportRepository] Error occured in DeleteReportedProductAsync : {ex}");
                return false;
            }
        }
    }
}
