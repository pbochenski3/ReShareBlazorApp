namespace ReShareBlazorApp.Api.Date
{
    public class Conversation
    {
        public int Id { get; set; }

        // Ogłoszenie, którego dotyczy rozmowa (brak dla rozmów z supportem).
        public int? ProductId { get; set; }
        public Product? Product { get; set; }

        // Dla zwykłej rozmowy: dwaj uczestnicy (kolejność nieistotna).
        // Dla rozmowy z supportem: User1 = administrator, który napisał, User2 = zgłaszający.
        public int User1Id { get; set; }
        public User User1 { get; set; } = null!;

        public int User2Id { get; set; }
        public User User2 { get; set; } = null!;

        public bool IsSupport { get; set; }

        // Zgłoszenie, w związku z którym support napisał do użytkownika.
        public int? ReportId { get; set; }
        public Report? Report { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastMessageAt { get; set; } = DateTime.UtcNow;

        public List<Message> Messages { get; set; } = new();
    }
}
