
using ReShareBlazorApp.Shared.DTOs;

namespace ReShareBlazorApp.Api.Interfaces.Services
{
    public interface IBadgeService
    {
        Task<List<BadgeDTO>> GetUserBadgesAsync(int userId);

        Task<List<BadgeDTO>> GetAllBadgesAsync();

        Task<bool> AddBadgeAsync(BadgeDTO badge);

        Task<bool> AwardBadgeAsync(int userId, string badgeName);
    }
}
