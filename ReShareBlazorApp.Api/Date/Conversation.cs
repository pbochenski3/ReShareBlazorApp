using ReShareBlazorApp.Shared.Enums;

namespace ReShareBlazorApp.Api.Date
{
    public class Conversation
    {
        public int Id { get; set; }
        public ConversationType Type { get; set; } = ConversationType.Direct;

        // Direct: mniejsze ID uczestnika. Support: ID użytkownika rozmawiającego z administracją.
        public int UserLowId { get; set; }

        // Direct: większe ID uczestnika. Support: null.
        public int? UserHighId { get; set; }

        public int? ProductId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastMessageAt { get; set; } = DateTime.UtcNow;

        public List<ChatMessage> Messages { get; set; } = new();
    }
}