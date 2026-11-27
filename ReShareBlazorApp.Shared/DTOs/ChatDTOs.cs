using System.ComponentModel.DataAnnotations;
using ReShareBlazorApp.Shared.Enums;

namespace ReShareBlazorApp.Shared.DTOs;

public static class ChatConstants
{
    public const int MaxMessageLength = 2000;
    public const int MaxAdminReplyLength = 1500;
    public const string HubPath = "/hubs/chat";
    public const string SupportDisplayName = "Administracja ODDAJ DALEJ";
}

public class ChatMessageDTO
{
    public int Id { get; set; }
    public int ConversationId { get; set; }
    public int SenderId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
    public bool IsRead { get; set; }
}

public class ConversationDTO
{
    public int Id { get; set; }
    public ConversationType Type { get; set; }
    public int? OtherUserId { get; set; }
    public string OtherUserName { get; set; } = string.Empty;
    public string? OtherUserAvatarUrl { get; set; }
    public int? ProductId { get; set; }
    public string? ProductName { get; set; }
    public string LastMessage { get; set; } = string.Empty;
    public DateTime LastMessageAt { get; set; }
    public int UnreadCount { get; set; }
}

public class StartConversationRequest
{
    public int RecipientId { get; set; }
    public int? ProductId { get; set; }
}

public class AdminReplyRequest
{
    [Required]
    [StringLength(1500, MinimumLength = 1)]
    public string Message { get; set; } = string.Empty;
}

public class AdminReplyDTO
{
    public int Id { get; set; }
    public string AdminName { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public string Recipient { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class ChatLogDTO
{
    public int Id { get; set; }
    public int? MessageId { get; set; }
    public int ConversationId { get; set; }
    public int SenderId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public int? RecipientId { get; set; }
    public string RecipientName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ChatLogPageDTO
{
    public List<ChatLogDTO> Items { get; set; } = new();
    public int Total { get; set; }
}

public class ChatLogContentDTO
{
    public string Content { get; set; } = string.Empty;
}