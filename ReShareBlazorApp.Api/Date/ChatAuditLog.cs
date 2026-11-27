using System.ComponentModel.DataAnnotations;

namespace ReShareBlazorApp.Api.Date;

// Szczegóły zdarzenia są szyfrowane; identyfikatory i czas pozostają jawne dla indeksowania/audytu.
public sealed class ChatAuditLog
{
    public long Id { get; set; }
    public int ConversationId { get; set; }
    public int? MessageId { get; set; }
    public int? ActorUserId { get; set; }
    [Required, MaxLength(40)] public string EventType { get; set; } = "";
    [Required] public string CipherText { get; set; } = "";
    [Required, MaxLength(32)] public string Nonce { get; set; } = "";
    [Required, MaxLength(32)] public string AuthTag { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
