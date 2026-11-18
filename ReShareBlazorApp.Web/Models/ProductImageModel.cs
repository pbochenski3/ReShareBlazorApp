namespace ReShareBlazorApp.Web.Models
{
    public class ProductImageModel
    {
        public string FileName { get; set; } = "";
        public string ContentType { get; set; } = "image/jpeg";
        public byte[] Data { get; set; } = Array.Empty<byte>();
        // data: URL użyty tylko do podglądu w formularzu
        public string PreviewSrc { get; set; } = "";
    }
}
