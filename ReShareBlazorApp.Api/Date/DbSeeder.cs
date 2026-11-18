using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ReShareBlazorApp.Api.Date.Enums;

namespace ReShareBlazorApp.Api.Date
{
    // Wrzuca do bazy przykładowych użytkowników i ogłoszenia, żeby było co oglądać i testować.
    // Odpala się przy każdym starcie API i dodaje tylko to, czego jeszcze brakuje (nie dubluje).
    public static class DbSeeder
    {
        public const string DemoPassword = "Haslo1234";

        // Zwraca: ile ogłoszeń dodano, ile jest w bazie razem i ewentualny błąd
        public static async Task<(int Added, int Total, string? Error)> SeedAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ReshareDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DbSeeder");

            try
            {
                // 1) czterech przykładowych użytkowników (wszyscy z hasłem Haslo1234) - tworzymy tych, których brakuje
                var anna = await GetOrCreateUser(userManager, "anna.kowalska", "anna@reshare.pl", 40);
                var marek = await GetOrCreateUser(userManager, "marek.nowak", "marek@reshare.pl", 25);
                var ewa = await GetOrCreateUser(userManager, "ewa.wisniewska", "ewa@reshare.pl", 12);
                var piotr = await GetOrCreateUser(userManager, "piotr.zielinski", "piotr@reshare.pl", 5);
                var sellers = new[] { anna, marek, ewa, piotr };

                // 2) ogłoszenia: (nazwa, cena, opis, kategoria, ważne za ile dni)
                var items = new List<(string Name, decimal Price, string Desc, ProductCategory Cat, int Days)>
                {
                    // --- Bilety ---
                    ("2 bilety na koncert – Kraków", 240, "Dwa bilety na koncert w TAURON Arenie, sektor płyta. Nie mogę być, oddaję w cenie zakupu.", ProductCategory.Tickets, 25),
                    ("Bilet na mecz Wisła Kraków – Cracovia", 80, "Derby Krakowa, trybuna odkryta, miejsce siedzące. Bilet elektroniczny, przekażę w aplikacji.", ProductCategory.Tickets, 14),
                    ("Karnet na festiwal Open'er – 3 dni", 650, "Karnet trzydniowy, można przepisać na swoje dane. Kupiony w ostatniej puli.", ProductCategory.Tickets, 60),
                    ("4 bilety do teatru – „Wesele”", 180, "Cztery miejsca na balkonie, spektakl w sobotę o 19:00. Cena za wszystkie cztery.", ProductCategory.Tickets, 9),
                    ("Bilet do Opery – „Jezioro łabędzie”", 220, "Jedno miejsce w parterze, rząd 8. Wydarzenie w ostatni piątek miesiąca.", ProductCategory.Tickets, 20),
                    ("Wieczór stand-up – 2 bilety", 90, "Dwa bilety na stand-up w klubie Kwadrat, stoliki przy scenie.", ProductCategory.Tickets, 11),
                    ("Voucher do kina na 2 osoby", 30, "Dwa bilety do Cinema City na dowolny seans 2D, ważne jeszcze kilka tygodni.", ProductCategory.Tickets, 30),
                    ("2 wejściówki na wystawę w muzeum", 0, "Oddam za darmo dwie wejściówki na wystawę czasową, nie zdążę z nich skorzystać.", ProductCategory.Tickets, 7),

                    // --- Rezerwacje ---
                    ("Rezerwacja stolika – sobota 19:00", 0, "Stolik dla 4 osób w modnej restauracji w centrum. Mam rezerwację, ale zmieniły mi się plany.", ProductCategory.Reservations, 6),
                    ("Voucher do SPA – 400 zł", 280, "Voucher o wartości 400 zł do hotelowego SPA, ważny do końca roku. Sprzedam taniej.", ProductCategory.Reservations, 40),
                    ("Nocleg w Zakopanem – 1 noc", 250, "Przepiszę rezerwację na pokój dwuosobowy ze śniadaniem, bezpłatna zmiana danych.", ProductCategory.Reservations, 18),
                    ("Kolacja dla dwojga – voucher", 150, "Voucher na trzydaniową kolację dla dwóch osób wraz z lampką wina.", ProductCategory.Reservations, 45),
                    ("Rezerwacja toru gokartowego", 60, "Godzina na torze w niedzielę o 15:00, do 6 osób. Przepisuję rezerwację.", ProductCategory.Reservations, 5),
                    ("Kort tenisowy – czwartek 18:00", 40, "Mam wykupiony kort na godzinę, nie dam rady dojechać. Oddaję rezerwację.", ProductCategory.Reservations, 3),
                    ("Voucher na masaż relaksacyjny", 120, "Masaż 60 minut, voucher imienny można przepisać. Ważny do końca marca.", ProductCategory.Reservations, 50),
                    ("Escape room dla 4 osób", 100, "Rezerwacja pokoju zagadek w sobotę o 17:30, cena za całą grupę.", ProductCategory.Reservations, 8),

                    // --- Wyjazdy i podróże ---
                    ("Wycieczka do Chorwacji – 7 dni", 3200, "Przepisuję rezerwację dla 2 osób, 7 nocy, apartament przy plaży, bez przelotu.", ProductCategory.Trips, 55),
                    ("Weekend w Zakopanem dla dwojga", 600, "Dwie noce w pensjonacie ze śniadaniami, termin do ustalenia w ciągu miesiąca.", ProductCategory.Trips, 35),
                    ("Last minute – Egipt, 8 dni", 2800, "Wczasy all inclusive dla jednej osoby, wylot z Warszawy. Zmiana nazwiska możliwa.", ProductCategory.Trips, 15),
                    ("Weekend w Pradze – hotel i bilety", 900, "Dwie noce w hotelu 4* w centrum plus bilety na metro i tramwaje.", ProductCategory.Trips, 28),
                    ("Dwa noclegi w Gdańsku", 450, "Apartament na Starówce dla dwóch osób, samodzielne zameldowanie.", ProductCategory.Trips, 22),
                    ("Rejs po Mazurach – wolne 2 miejsca", 1500, "Czarter jachtu na tydzień, mamy jeszcze dwa wolne miejsca w załodze.", ProductCategory.Trips, 70),
                    ("Wyjazd narciarski – Dolomity", 2200, "Sześć dni, skipass w cenie, wyjazd autokarem z Katowic. Dwa miejsca.", ProductCategory.Trips, 80),

                    // --- Transport ---
                    ("Wolne miejsce Warszawa → Kraków", 50, "Jadę w piątek o 18:00 sam, mam 3 wolne miejsca. Wspólne koszty paliwa.", ProductCategory.Transports, 4),
                    ("Przejazd Gdańsk → Wrocław", 70, "Wyjazd w sobotę rano, komfortowe auto, dwa wolne miejsca.", ProductCategory.Transports, 6),
                    ("Dowóz na lotnisko Chopina", 30, "Zabiorę z Mokotowa lub Ursynowa, wylot w poniedziałek o 6:00.", ProductCategory.Transports, 10),
                    ("Bus do Zakopanego – wolne miejsca", 40, "Wyjazd w piątek po południu z Krakowa, powrót w niedzielę wieczorem.", ProductCategory.Transports, 7),
                    ("Przewóz mebli dostawczakiem", 120, "Przewiozę szafę, kanapę czy pralkę po mieście, mam pas transportowy.", ProductCategory.Transports, 30),
                    ("Poznań → Berlin – 2 miejsca", 90, "Jadę w czwartek rano, wracam w niedzielę. Wolne dwa miejsca w aucie.", ProductCategory.Transports, 12),
                    ("Wspólny dojazd na koncert do Katowic", 25, "Wyjeżdżamy z Krakowa o 16:00, powrót po koncercie. Dwa miejsca.", ProductCategory.Transports, 16),

                    // --- Usługi ---
                    ("Korepetycje z matematyki – wolny termin", 80, "Liceum i matura, 60 minut, możliwa nauka zdalna. Wolne wtorki i czwartki po 17:00.", ProductCategory.Services, 60),
                    ("Sesja zdjęciowa portretowa", 250, "Sesja plenerowa do 1,5 godz., 20 obrobionych zdjęć. Wolny termin w weekend.", ProductCategory.Services, 45),
                    ("Lekcja jazdy z instruktorem", 100, "Godzina jazdy po mieście, jeden wolny termin w tym tygodniu.", ProductCategory.Services, 10),
                    ("Opieka nad psem podczas wyjazdu", 40, "Zajmę się Twoim psem w moim domu z ogródkiem, cena za dobę.", ProductCategory.Services, 90),
                    ("Wolny termin u fryzjera – piątek", 90, "Strzyżenie i stylizacja, mam wolne okienko w piątek o 14:00.", ProductCategory.Services, 4),
                    ("Naprawa roweru – szybki serwis", 60, "Regulacja hamulców i przerzutek, wymiana dętki. Odbiór tego samego dnia.", ProductCategory.Services, 40),
                    ("Lekcje gitary dla początkujących", 70, "Pierwsza lekcja próbna w promocji, wolne terminy w środy i soboty.", ProductCategory.Services, 60),
                    ("Hydraulik – wolny termin jutro", 150, "Drobne naprawy, wymiana baterii, udrażnianie. Mam wolne okno jutro rano.", ProductCategory.Services, 2),
                    ("Korepetycje z angielskiego", 75, "Konwersacje i przygotowanie do egzaminów, wolne wieczory w tygodniu.", ProductCategory.Services, 60),
                };

                // 3) dodajemy tylko te, których jeszcze nie ma (po nazwie), rozdając je po kolei między użytkowników
                var existingNames = await db.Products.Select(p => p.Name).ToListAsync();
                var now = DateTime.Now;
                int added = 0;
                for (int i = 0; i < items.Count; i++)
                {
                    var item = items[i];
                    if (existingNames.Contains(item.Name))
                        continue; // już jest w bazie
                    db.Products.Add(new Product
                    {
                        Name = item.Name,
                        Price = item.Price,
                        Description = item.Desc,
                        Category = item.Cat,
                        UserId = sellers[i % sellers.Length].Id,   // kolejny sprzedawca po kolei
                        AddedAt = now.AddHours(-(i * 7 + 1)),      // każde starsze o ok. 7 godzin
                        ExpiresAt = now.AddDays(item.Days),
                    });
                    added++;
                }
                await db.SaveChangesAsync();
                var total = await db.Products.CountAsync();
                logger.LogWarning("DbSeeder: dodano {Added} ogłoszeń, razem w bazie: {Total}. Konta demo: anna@reshare.pl, marek@reshare.pl, ewa@reshare.pl, piotr@reshare.pl, hasło {Password}", added, total, DemoPassword);
                return (added, total, null);
            }
            catch (Exception ex)
            {
                // seeder nie może wywrócić aplikacji, ale błąd pokazujemy wyraźnie w konsoli
                logger.LogError(ex, "DbSeeder: nie udało się dodać przykładowych danych.");
                return (0, 0, ex.GetBaseException().Message);
            }
        }

        private static async Task<User> GetOrCreateUser(UserManager<User> userManager, string userName, string email, int daysAgo)
        {
            var existing = await userManager.FindByEmailAsync(email);
            if (existing != null)
                return existing;
            var user = new User
            {
                UserName = userName,
                Email = email,
                EmailConfirmed = true,
                CreatedAt = DateTime.Now.AddDays(-daysAgo),
                IsActive = true
            };
            var result = await userManager.CreateAsync(user, DemoPassword);
            if (!result.Succeeded)
                throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
            return user;
        }
    }
}
