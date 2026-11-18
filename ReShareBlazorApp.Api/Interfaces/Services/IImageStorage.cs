namespace ReShareBlazorApp.Api.Interfaces.Services
{
    public interface IImageStorage
    {
        /// <summary>Zapisuje plik na dysku i zwraca wygenerowaną nazwę pliku.</summary>
        Task<string> SaveAsync(byte[] data, string extension);
        /// <summary>Zwraca zawartość pliku albo null, gdy plik nie istnieje.</summary>
        Task<byte[]?> ReadAsync(string storedFileName);
        void Delete(string storedFileName);
    }
}
