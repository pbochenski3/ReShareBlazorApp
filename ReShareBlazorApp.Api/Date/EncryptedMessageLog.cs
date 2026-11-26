using System.ComponentModel.DataAnnotations;

namespace ReShareBlazorApp.Api.Date;

public class EncryptedMessageLog
{
    public int Id { get; set; }

    public int ConversationId { get; set; }
    public int? MessageId { get; set; }

    public int SenderId { get; set; }
    [MaxLength(256)]
    public string SenderName { get; set; } = string.Empty;

    public int RecipientId { get; set; }
    [MaxLength(256)]
    public string RecipientName { get; set; } = string.Empty;

    [MaxLength(50)]
    public string EventType { get; set; } = string.Empty;

    /// <summary>Zaszyfrowany ładunek JSON (AES-256-GCM) zawierający treść i metadane wiadomości.</summary>
    public required string EncryptedPayload { get; set; }

    /// <summary>Skrót SHA-256 nieszyfrowanego ładunku JSON do weryfikacji integralności.</summary>
    [MaxLength(64)]
    public required string PayloadSha256 { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
