using ReShareBlazorApp.Api.Date;

namespace ReShareBlazorApp.Api.Interfaces.Repositories
{
    public interface IBadgeRepository
    {
        Task<List<Badge>> ViewAllBadgesAsync();

        Task<List<Badge>> ViewUnlockBadgeAsync(int userId);

        Task<bool> AwardBadgeAsync(int userId, string badgeName);

        Task<bool> AwardBadgeToUserAsync(int userId, int badgeId);

        Task<bool> RemoveBadgeFromUserAsync(int userId, int badgeId);
        Task<bool> AddBadgeAsync(Badge badge);

        Task<bool> AwardBadgeAsync(int userId, string badgeName);
    }
}