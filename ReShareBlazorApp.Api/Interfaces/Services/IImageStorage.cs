namespace ReShareBlazorApp.Api.Interfaces.Services
{
    public interface IImageStorage
    {
        /// <summary>Zapisuje plik na dysku i zwraca wygenerowaną nazwę pliku.</summary>
        Task<string> SaveAsync(byte[] data, string extension, string folder);
        /// <summary>Zwraca zawartość pliku albo null, gdy plik nie istnieje.</summary>
        Task<byte[]?> ReadAsync(string storedFileName, string folder);
        void Delete(string storedFileName, string folder);
        string GetFolderPath(string folder);
    }
}
