using Microsoft.EntityFrameworkCore;
using ReShareBlazorApp.Api.Date;
using ReShareBlazorApp.Api.Interfaces.Services;
using ReShareBlazorApp.Shared.Enums;
using System.Globalization;
using System.Text;

namespace ReShareBlazorApp.Api.Services
{
    public class NotificationService : INotificationService
    {
        private readonly ReshareDbContext _context;

        public NotificationService(ReshareDbContext context)
        {
            _context = context;
        }

        public async Task CreateAsync(
            int userId,
            NotificationType type,
            string title,
            string message,
            int? productId = null,
            string? actorName = null,
            CancellationToken cancellationToken = default)
        {
            _context.Notifications.Add(new Notification
            {
                UserId = userId,
                ProductId = productId,
                ActorName = string.IsNullOrWhiteSpace(actorName) ? null : Limit(actorName.Trim(), 256),
                Type = type,
                Title = Limit(title, 200),
                Message = Limit(message, 1000),
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task CreateForMatchingWantedListingsAsync(
            Product offer,
            CancellationToken cancellationToken = default)
        {
            if (offer.Type == ProductType.LookingFor)
                return;

            var wantedListings = await _context.Products
                .AsNoTracking()
                .Where(product => product.Type == ProductType.LookingFor
                    && product.UserId != offer.UserId)
                .Select(product => new { product.UserId, product.Name })
                .ToListAsync(cancellationToken);

            var offerWords = GetWords(offer.Name);
            var matches = wantedListings
                .Where(wanted =>
                {
                    var wantedWords = GetWords(wanted.Name);
                    return wantedWords.Count > 0 && wantedWords.Overlaps(offerWords);
                })
                .GroupBy(wanted => wanted.UserId)
                .Select(group => group.First())
                .Select(wanted => new Notification
                {
                    UserId = wanted.UserId,
                    ProductId = offer.Id,
                    Type = NotificationType.OfferMatch,
                    Title = "Znaleziono szukany przedmiot",
                    Message = Limit(
                        $"Oferta „{offer.Name}” pasuje do Twojego poszukiwania „{wanted.Name}”.",
                        1000),
                    CreatedAt = DateTime.UtcNow
                })
                .ToList();

            if (matches.Count == 0)
                return;

            _context.Notifications.AddRange(matches);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task CreateForMatchingOffersAsync(
            Product wantedListing,
            CancellationToken cancellationToken = default)
        {
            if (wantedListing.Type != ProductType.LookingFor)
                return;

            var wantedWords = GetWords(wantedListing.Name);
            if (wantedWords.Count == 0)
                return;

            var offers = await _context.Products
                .AsNoTracking()
                .Where(product =>
                    (product.Type == ProductType.Sell || product.Type == ProductType.GiveAway)
                    && product.UserId != wantedListing.UserId)
                .Select(product => new { product.Id, product.UserId, product.Name })
                .ToListAsync(cancellationToken);

            var matches = offers
                .Where(offer => GetWords(offer.Name).Overlaps(wantedWords))
                .GroupBy(offer => offer.UserId)
                .Select(group => group.First())
                .Select(offer => new Notification
                {
                    UserId = wantedListing.UserId,
                    ProductId = offer.Id,
                    Type = NotificationType.OfferMatch,
                    Title = "Znaleziono szukany przedmiot",
                    Message = Limit(
                        $"Oferta „{offer.Name}” pasuje do Twojego poszukiwania „{wantedListing.Name}”.",
                        1000),
                    CreatedAt = DateTime.UtcNow
                })
                .ToList();

            if (matches.Count == 0)
                return;

            _context.Notifications.AddRange(matches);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<List<Notification>> GetForUserAsync(
            int userId,
            CancellationToken cancellationToken = default)
        {
            await CreateMissingOfferMatchNotificationsAsync(userId, cancellationToken);

            return await _context.Notifications
                .AsNoTracking()
                .Where(notification => notification.UserId == userId
                    && (notification.ProductId == null
                        || _context.Products.Any(product =>
                            product.Id == notification.ProductId.Value)))
                .OrderByDescending(notification => notification.CreatedAt)
                .Take(100)
                .ToListAsync(cancellationToken);
        }

        private async Task CreateMissingOfferMatchNotificationsAsync(
            int userId,
            CancellationToken cancellationToken)
        {
            var wantedNames = await _context.Products
                .AsNoTracking()
                .Where(product => product.UserId == userId
                    && product.Type == ProductType.LookingFor)
                .Select(product => product.Name)
                .ToListAsync(cancellationToken);

            if (wantedNames.Count == 0)
                return;

            var offers = await _context.Products
                .AsNoTracking()
                .Where(product => product.UserId != userId
                    && (product.Type == ProductType.Sell || product.Type == ProductType.GiveAway))
                .Select(product => new { product.Id, product.UserId, product.Name })
                .ToListAsync(cancellationToken);

            var existingOfferIds = await _context.Notifications
                .AsNoTracking()
                .Where(notification => notification.UserId == userId
                    && notification.Type == NotificationType.OfferMatch
                    && notification.ProductId.HasValue)
                .Select(notification => notification.ProductId!.Value)
                .ToListAsync(cancellationToken);

            var existingOfferOwners = offers
                .Where(offer => existingOfferIds.Contains(offer.Id))
                .Select(offer => offer.UserId)
                .ToHashSet();

            var matches = new List<Notification>();
            foreach (var offer in offers)
            {
                if (existingOfferIds.Contains(offer.Id)
                    || existingOfferOwners.Contains(offer.UserId))
                {
                    continue;
                }

                var offerWords = GetWords(offer.Name);
                var matchingWantedName = wantedNames.FirstOrDefault(wantedName =>
                    GetWords(wantedName).Overlaps(offerWords));

                if (matchingWantedName is null)
                    continue;

                existingOfferOwners.Add(offer.UserId);
                matches.Add(new Notification
                {
                    UserId = userId,
                    ProductId = offer.Id,
                    Type = NotificationType.OfferMatch,
                    Title = "Znaleziono szukany przedmiot",
                    Message = Limit(
                        $"Oferta „{offer.Name}” pasuje do Twojego poszukiwania „{matchingWantedName}”.",
                        1000),
                    CreatedAt = DateTime.UtcNow
                });
            }

            if (matches.Count == 0)
                return;

            _context.Notifications.AddRange(matches);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<bool> MarkAsReadAsync(
            int notificationId,
            int userId,
            CancellationToken cancellationToken = default)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(
                    item => item.Id == notificationId && item.UserId == userId,
                    cancellationToken);

            if (notification is null)
                return false;

            if (!notification.IsRead)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync(cancellationToken);
            }

            return true;
        }

        public async Task<bool> DeleteAsync(
            int notificationId,
            int userId,
            CancellationToken cancellationToken = default)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(
                    item => item.Id == notificationId && item.UserId == userId,
                    cancellationToken);

            if (notification is null)
                return false;

            _context.Notifications.Remove(notification);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        private static HashSet<string> GetWords(string value)
        {
            var normalized = string.Concat(value
                .Normalize(NormalizationForm.FormD)
                .Where(character => CharUnicodeInfo.GetUnicodeCategory(character)
                    != UnicodeCategory.NonSpacingMark))
                .Normalize(NormalizationForm.FormC)
                .ToLowerInvariant();

            var words = new List<string>();
            var currentWord = new StringBuilder();
            foreach (var character in normalized)
            {
                if (char.IsLetterOrDigit(character))
                {
                    currentWord.Append(character);
                }
                else if (currentWord.Length > 0)
                {
                    words.Add(currentWord.ToString());
                    currentWord.Clear();
                }
            }

            if (currentWord.Length > 0)
                words.Add(currentWord.ToString());

            return words.ToHashSet(StringComparer.Ordinal);
        }

        private static string Limit(string value, int maxLength) =>
            value.Length <= maxLength ? value : value[..maxLength];
    }
}
