using ReShareBlazorApp.Api.Interfaces.Services;

namespace ReShareBlazorApp.Api.Services
{
    public class DiskImageStorage : IImageStorage
    {
        private readonly string _rootPath;

        public DiskImageStorage(IConfiguration configuration, IWebHostEnvironment environment)
        {
            var configured = configuration["ImageStorage:Path"];
            if (string.IsNullOrWhiteSpace(configured))
                configured = Path.Combine("Uploads", "products");

            // ścieżka względna liczona od katalogu projektu Api, bezwzględna działa bez zmian
            _rootPath = Path.GetFullPath(configured, environment.ContentRootPath);
            Directory.CreateDirectory(_rootPath);
        }

        public async Task<string> SaveAsync(byte[] data, string extension)
        {
            var storedFileName = $"{Guid.NewGuid():N}{extension}";
            await File.WriteAllBytesAsync(GetFullPath(storedFileName), data);
            return storedFileName;
        }

        public async Task<byte[]?> ReadAsync(string storedFileName)
        {
            var path = GetFullPath(storedFileName);
            if (!File.Exists(path))
                return null;

            return await File.ReadAllBytesAsync(path);
        }

        public void Delete(string storedFileName)
        {
            var path = GetFullPath(storedFileName);
            if (File.Exists(path))
                File.Delete(path);
        }

        // Path.GetFileName odcina ewentualne ścieżki (ochrona przed ../)
        private string GetFullPath(string storedFileName)
            => Path.Combine(_rootPath, Path.GetFileName(storedFileName));
    }
}
