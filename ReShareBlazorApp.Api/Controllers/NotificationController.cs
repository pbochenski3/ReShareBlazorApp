using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReShareBlazorApp.Api.Interfaces.Services;
using ReShareBlazorApp.Shared.DTOs;
using System.Security.Claims;

namespace ReShareBlazorApp.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpGet]
        public async Task<ActionResult<List<NotificationDTO>>> GetNotifications(
            CancellationToken cancellationToken)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized();

            var notifications = await _notificationService.GetForUserAsync(userId, cancellationToken);
            return Ok(notifications.Select(notification => new NotificationDTO
            {
                Id = notification.Id,
                ProductId = notification.ProductId,
                Type = notification.Type,
                Title = notification.Title,
                Message = notification.Message,
                CreatedAt = notification.CreatedAt,
                IsRead = notification.IsRead
            }).ToList());
        }

        [HttpPut("{id:int}/read")]
        public async Task<IActionResult> MarkAsRead(
            int id,
            CancellationToken cancellationToken)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized();

            var marked = await _notificationService.MarkAsReadAsync(id, userId, cancellationToken);
            return marked ? NoContent() : NotFound();
        }

        private bool TryGetUserId(out int userId)
        {
            return int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
        }
    }
}
