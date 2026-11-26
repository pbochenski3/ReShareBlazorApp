using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReShareBlazorApp.Api.Interfaces.Services;
using ReShareBlazorApp.Shared;
using ReShareBlazorApp.Shared.DTOs;
using System.Security.Claims;

namespace ReShareBlazorApp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReportController : ControllerBase
    {
        private readonly IReportService _reportService;
        private readonly IActivityLogger _activityLogger;
        private readonly IChatService _chatService;

        public ReportController(
            IReportService reportService,
            IActivityLogger activityLogger,
            IChatService chatService)
        {
            _reportService = reportService;
            _activityLogger = activityLogger;
            _chatService = chatService;
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
            await _activityLogger.LogAsync(User, ActivityActions.ReportAdded,
                $"Zgłoszono ogłoszenie (id: {report.ProductId}), kategoria: {report.Category}",
                "Product", report.ProductId);
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
        [HttpGet("{id:int}/threads")]
        public async Task<ActionResult<AdminReportThreadsDTO>> GetReportThreads(int id)
        {
            var threads = await _chatService.GetReportThreadsAsync(id);
            if (threads is null)
                return NotFound();

            return Ok(threads);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("{id:int}/reply")]
        public async Task<ActionResult<AdminReplyResultDTO>> ReplyToReport(
            int id,
            [FromBody] AdminReportReplyRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var adminUserId))
                return Unauthorized();

            var result = await _chatService.SendAdminReportReplyAsync(User, adminUserId, id, request);
            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteReport(int id)
        {
            var result = await _reportService.DeleteReportAsync(id);
            if (!result)
                return NotFound();
            await _activityLogger.LogAsync(User, ActivityActions.ReportDeleted,
                $"Usunięto zgłoszenie (id: {id})", "Report", id);
            return Ok();
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id:int}/product")]
        public async Task<IActionResult> DeleteReportedProduct(int id)
        {
            var result = await _reportService.DeleteReportedProductAsync(id);
            if (!result)
                return NotFound();
            await _activityLogger.LogAsync(User, ActivityActions.ReportedProductDeleted,
                $"Usunięto ogłoszenie zgłoszone w zgłoszeniu (id: {id})", "Report", id);
            return Ok();
        }
    }
}
