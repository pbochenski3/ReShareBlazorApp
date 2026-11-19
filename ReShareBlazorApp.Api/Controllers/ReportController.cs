using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReShareBlazorApp.Api.Interfaces.Services;
using ReShareBlazorApp.Shared.DTOs;
using System.Security.Claims;

namespace ReShareBlazorApp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReportController : ControllerBase
    {
        private readonly IReportService _reportService;
        public ReportController(IReportService reportService)
        {
            _reportService = reportService;
        }
        [Authorize]
        [HttpPost("addReport")]
        public async Task<IActionResult> AddReport(ReportDTO report)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return Unauthorized();
            int id = int.Parse(userId);
            report.UserId = id;
            var result = await _reportService.AddReportAsync(report);
            if (!result)
            {
                return BadRequest();
            }
            return Ok();
        }
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<ActionResult<List<ReportDTO>>> GetReports()
        {
            var reports = await _reportService.GetReportsAsync();
            return Ok(reports);
        }
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteReport(int id)
        {
            var result = await _reportService.DeleteReportAsync(id);
            if (!result)
                return NotFound();
            return Ok();
        }
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id:int}/product")]
        public async Task<IActionResult> DeleteReportedProduct(int id)
        {
            var result = await _reportService.DeleteReportedProductAsync(id);
            if (!result)
                return NotFound();
            return Ok();
        }
    }
}
