using System.ComponentModel.DataAnnotations;

namespace ReShareBlazorApp.Api.Date;

public class ChatMessage
{
    public int Id { get; set; }

    public int ConversationId { get; set; }
    public ChatConversation Conversation { get; set; } = null!;

    public int SenderId { get; set; }
    public User Sender { get; set; } = null!;

    public int RecipientId { get; set; }
    public User Recipient { get; set; } = null!;

    /// <summary>Treść wiadomości zaszyfrowana algorytmem AES-256-GCM.</summary>
    public required string EncryptedContent { get; set; }

    /// <summary>Skrót SHA-256 oryginalnej treści w formacie szesnastkowym.</summary>
    [MaxLength(64)]
    public required string ContentHash { get; set; }

    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
    public bool IsAdminMessage { get; set; }
}
