using ReShareBlazorApp.Api.Date;
using ReShareBlazorApp.Shared.Enums;

namespace ReShareBlazorApp.Api.Interfaces.Services
{
    public interface INotificationService
    {
        Task CreateAsync(
            int userId,
            NotificationType type,
            string title,
            string message,
            int? productId = null,
            CancellationToken cancellationToken = default);

        Task CreateForMatchingWantedListingsAsync(
            Product offer,
            CancellationToken cancellationToken = default);

        Task CreateForMatchingOffersAsync(
            Product wantedListing,
            CancellationToken cancellationToken = default);

        Task<List<Notification>> GetForUserAsync(
            int userId,
            CancellationToken cancellationToken = default);

        Task<bool> MarkAsReadAsync(
            int notificationId,
            int userId,
            CancellationToken cancellationToken = default);

        Task<bool> DeleteAsync(
            int notificationId,
            int userId,
            CancellationToken cancellationToken = default);
    }
}
