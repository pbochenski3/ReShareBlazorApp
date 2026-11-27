using System.ComponentModel.DataAnnotations;

namespace ReShareBlazorApp.Api.Date;

public sealed class ChatConversation
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastMessageAt { get; set; } = DateTime.UtcNow;
    public bool IsSupport { get; set; }
    [MaxLength(80)] public string? SupportReference { get; set; }
    public ICollection<ChatParticipant> Participants { get; set; } = new List<ChatParticipant>();
    public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
}
