namespace ReShareBlazorApp.Api.Date
{
    // Ulubione ogłoszenie danego użytkownika (tabela łącząca User <-> Product)
    public class Favorite
    {
        public int UserId { get; set; }                  // kto dodał do ulubionych
        public User User { get; set; } = null!;          // nawigacja do użytkownika
        public int ProductId { get; set; }               // które ogłoszenie
        public Product Product { get; set; } = null!;    // nawigacja do ogłoszenia
        public DateTime AddedAt { get; set; } = DateTime.Now; // kiedy dodano
    }
}
