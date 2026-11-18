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
                configured = "Uploads";

            // ścieżka względna liczona od katalogu projektu Api, bezwzględna działa bez zmian
            _rootPath = Path.GetFullPath(configured, environment.ContentRootPath);
            Directory.CreateDirectory(_rootPath);
        }

        public async Task<string> SaveAsync(byte[] data, string extension, string folder)
        {
            var storedFileName = $"{Guid.NewGuid():N}{extension}";
            await File.WriteAllBytesAsync(GetFullPath(storedFileName, folder), data);
            return storedFileName;
        }

        public async Task<byte[]?> ReadAsync(string storedFileName, string folder)
        {
            var path = GetFullPath(storedFileName, folder);
            if (!File.Exists(path))
                return null;

            return await File.ReadAllBytesAsync(path);
        }

        public void Delete(string storedFileName, string folder)
        {
            var path = GetFullPath(storedFileName, folder);
            if (File.Exists(path))
                File.Delete(path);
        }

        // Path.GetFileName odcina ewentualne ścieżki (ochrona przed ../)
        public string GetFolderPath(string folder)
        {
            var safeFolder = Path.GetFileName(folder);
            var path = Path.Combine(_rootPath, safeFolder);
            Directory.CreateDirectory(path);
            return path;
        }

        private string GetFullPath(string storedFileName, string folder)
            => Path.Combine(GetFolderPath(folder), Path.GetFileName(storedFileName));
    }
}
