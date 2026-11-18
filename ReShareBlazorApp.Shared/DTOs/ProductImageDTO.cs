namespace ReShareBlazorApp.Shared.DTOs
{
    /// <summary>
    /// Zdjęcie ogłoszenia – używane przy wysyłaniu (Data + ContentType)
    /// oraz przy pobieraniu pojedynczego zdjęcia z API.
    /// </summary>
    public class ProductImageDTO
    {
        public int Id { get; set; }
        public string FileName { get; set; } = "";
        public string ContentType { get; set; } = "";
        public byte[] Data { get; set; } = Array.Empty<byte>();
    }
}
