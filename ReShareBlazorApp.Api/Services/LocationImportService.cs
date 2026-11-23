using Microsoft.EntityFrameworkCore;
using ReShareBlazorApp.Api.Date;
using System.Text;

namespace ReShareBlazorApp.Api.Services
{
    public class LocationImportService
    {
     
        public static async Task ImportAsync(
            ReshareDbContext _context,
            string contentRoothPath)
        {
            if(await _context.Locations.AnyAsync())
            {
                return;
            }
            var filePath = Path.Combine(
                contentRoothPath,
                "SIMC.csv");
            if(!File.Exists(filePath))
            {
                throw new FileNotFoundException(
                    "Nie znaleziono pliku SIMC.csv.",
                    filePath);
            }
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var locations = new HashSet<string>(
                StringComparer.OrdinalIgnoreCase
                );
            using var reader = new StreamReader(
                filePath,
                Encoding.GetEncoding(1250));
            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                var columns = line.Split(';');

                if (columns.Length <= 6)
                    continue;

                if (columns[0].Trim('"').Trim() == "WOJ")
                    continue;

                var name = columns[6].Trim('"').Trim();

                if (!string.IsNullOrWhiteSpace(name))
                {
                    locations.Add(name);
                }
            }

            var locationList = locations
                .Select(name => new Location
                {
                    Name = name
                })
                .ToList();

            for (int i = 0; i < locationList.Count; i += 2000)
            {
                var batch = locationList
                    .Skip(i)
                    .Take(2000);

                await _context.Locations.AddRangeAsync(batch);
                await _context.SaveChangesAsync();
            }
            Console.WriteLine($"Zaimportowano {locationList.Count} miejscowości.");

        }
    }
}
