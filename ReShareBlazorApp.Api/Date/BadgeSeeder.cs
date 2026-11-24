using Microsoft.EntityFrameworkCore;
using ReShareBlazorApp.Api.Date;

namespace ReShareBlazorApp.Api.Data
{
    public static class BadgeSeeder
    {
        public static async Task SeedBadgesAsync(ReshareDbContext context)
        {
            var badges = new List<Badge>
            {
                new Badge
                {
                    Name = "Pierwsze koty za płoty",
                    Description = "Za rejestrację konta.",
                    Icon = "😸"
                },
                new Badge
                {
                    Name = "Pierwsze ogłoszenie",
                    Description = "Za dodanie pierwszego ogłoszenia.",
                    Icon = "🥇"
                },
                new Badge
                {
                    Name = "Aktywny sprzedawca",
                    Description = "Za dodanie 5 ogłoszeń.",
                    Icon = "🏆"
                },
                new Badge
                {
                    Name = "Król wśród kupujących",
                    Description = "Za dostanie 5 opini od innych użytkowników.",
                    Icon = "🤴"
                },
                new Badge
                {
                    Name = "Miłość od pierwszego wejrzenia",
                    Description = "Dodanie do ulubionych pierwszego ogłoszenia.",
                    Icon = "💕"
                },
                new Badge
                {
                    Name = "Zakochany w produktach",
                    Description = "Dodanie 10 produktów do ulubionych.",
                    Icon = "💞"
                },
                new Badge
                {
                    Name = "Pierwsza sprzedaż",
                    Description = "Za wykonanie pierwszej sprzedaży.",
                    Icon = "🛍"
                },
                new Badge
                {
                    Name = "Mistrz recenzji",
                    Description = "Za wystawienie 10 opinii innym użytkownikom.",
                    Icon = "💬"
                },
                new Badge
                {
                    Name = "Pierwsza opinia",
                    Description = "Za wystawienie pierwszej opinii.",
                    Icon = "💌"
                },

                new Badge
                {
                    Name = "Diamentowy sprzedawca",
                    Description = "Za wykonanie 10 zakończonych sprzedaży.",
                    Icon = "💎"
                },

            };

            foreach (var badge in badges)
            {
                bool exists = await context.Badges
                    .AnyAsync(b => b.Name == badge.Name);

                if (!exists)
                {
                    context.Badges.Add(badge);
                }
            }

            await context.SaveChangesAsync();
        }
    }
}
