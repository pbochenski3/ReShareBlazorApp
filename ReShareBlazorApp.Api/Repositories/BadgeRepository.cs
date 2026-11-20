using Microsoft.EntityFrameworkCore;
using ReShareBlazorApp.Api.Date;
using ReShareBlazorApp.Api.Interfaces.Repositories;

namespace ReShareBlazorApp.Api.Repositories
{
    public class BadgeRepository : IBadgeRepository
    {
        private readonly ReshareDbContext _context;
        private readonly ILogger<BadgeRepository> _logger;
        public BadgeRepository(ReshareDbContext context, ILogger<BadgeRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<Badge>> ViewAllBadgesAsync()
        {
            try
            {
                return await _context.Badges.ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in ViewAllBadgesAsync");
                return new List<Badge>();
            }
        }
        public async Task<List<Badge>> ViewUnlockBadgeAsync(int userId)
        {
            try
            {
                return await _context.UserBadges
                    .Where(ub => ub.UserId == userId)
                    .Include(ub => ub.Badge)
                    .Select(ub => ub.Badge)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in ViewUnlockBadgeAsync");
                return new List<Badge>();
            }
        }

        public async Task<bool> AddBadgeAsync(Badge badge)
        {
            try
            {
                await _context.Badges.AddAsync(badge);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in AddBadgeAsync");
                return false;
            }
        }

    }
}
