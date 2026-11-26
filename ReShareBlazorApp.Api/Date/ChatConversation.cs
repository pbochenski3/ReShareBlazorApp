using System.ComponentModel.DataAnnotations;
using ReShareBlazorApp.Shared.Enums;

namespace ReShareBlazorApp.Api.Date;

public class ChatConversation
{
    public int Id { get; set; }

    public int User1Id { get; set; }
    public User User1 { get; set; } = null!;

    public int User2Id { get; set; }
    public User User2 { get; set; } = null!;

    public ChatConversationType ConversationType { get; set; } = ChatConversationType.Direct;

    public int? ProductId { get; set; }
    public Product? Product { get; set; }

    public int? ContactMessageId { get; set; }
    public ContactMessage? ContactMessage { get; set; }

    public int? ReportId { get; set; }
    public Report? Report { get; set; }

    [MaxLength(200)]
    public string? Subject { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastMessageAt { get; set; } = DateTime.UtcNow;

    public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
}
