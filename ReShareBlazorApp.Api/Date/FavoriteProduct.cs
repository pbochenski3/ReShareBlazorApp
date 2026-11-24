namespace ReShareBlazorApp.Api.Date
{
    // Ogłoszenie dodane przez użytkownika do ulubionych (gwiazdka).
    public class FavoriteProduct
    {
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;

        public DateTime AddedAt { get; set; } = DateTime.Now;
    }
}
