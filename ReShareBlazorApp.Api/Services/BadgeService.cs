using ReShareBlazorApp.Api.Interfaces.Repositories;
using ReShareBlazorApp.Api.Interfaces.Services;
using ReShareBlazorApp.Shared.DTOs;

namespace ReShareBlazorApp.Api.Services
{
    public class BadgeService : IBadgeService
    {
        private readonly IBadgeRepository _badgeRepository;
        private readonly ILogger<BadgeService> _logger;

        public BadgeService(
            IBadgeRepository badgeRepository,
            ILogger<BadgeService> logger)
        {
            _badgeRepository = badgeRepository;
            _logger = logger;
        }

        public async Task<List<BadgeDTO>> GetUserBadgesAsync(int userId)
        {
            var allBadges = await _badgeRepository.ViewAllBadgesAsync();
            var unlockedBadges = await _badgeRepository.ViewUnlockBadgeAsync(userId);

            return allBadges.Select(b => new BadgeDTO
            {
                Id = b.Id,
                Name = b.Name,
                Description = b.Description,
                Icon = b.Icon,
                IsEarned = unlockedBadges.Any(ub => ub.Id == b.Id)
            }).ToList();
        }

        public async Task<List<BadgeDTO>> GetAllBadgesAsync()
        {
            var allBadges = await _badgeRepository.ViewAllBadgesAsync();

            return allBadges.Select(b => new BadgeDTO
            {
                Id = b.Id,
                Name = b.Name,
                Description = b.Description,
                Icon = b.Icon,
                IsEarned = false
            }).ToList();
        }

        public async Task<bool> AwardBadgeAsync(int userId, string badgeName)
        {
            var awarded = await _badgeRepository.AwardBadgeAsync(userId, badgeName);

            if (!awarded)
            {
                _logger.LogWarning(
                    "Nie udało się przyznać odznaki {BadgeName} użytkownikowi {UserId}.",
                    badgeName, userId);
            }

            return awarded;
        }

        public async Task<bool> AwardBadgeToUserAsync(int userId, int badgeId)
        {
            return await _badgeRepository.AwardBadgeToUserAsync(userId, badgeId);
        }

        public async Task<bool> RemoveBadgeFromUserAsync(int userId, int badgeId)
        {
            return await _badgeRepository.RemoveBadgeFromUserAsync(userId, badgeId);
        }
        // Dodawanie nowej odznaki
        public async Task<bool> AddBadgeAsync(BadgeDTO badge)
        {
            var newBadge = new Badge
            {
                Name = badge.Name,
                Description = badge.Description,
                Icon = badge.Icon
            };

            return await _badgeRepository.AddBadgeAsync(newBadge);
        }

        public async Task<bool> AwardBadgeAsync(int userId, string badgeName)
        {
            var awarded = await _badgeRepository.AwardBadgeAsync(userId, badgeName);

            if (!awarded)
            {
                _logger.LogWarning("Nie udało się przyznać odznaki {BadgeName} użytkownikowi {UserId}.", badgeName, userId);
            }

            return awarded;
        }
    }
}
