using System.ComponentModel.DataAnnotations;
using ReShareBlazorApp.Shared.Enums;

namespace ReShareBlazorApp.Shared.DTOs;

public class ChatConversationDTO
{
    public int Id { get; set; }
    public int OtherUserId { get; set; }
    public string OtherUserName { get; set; } = string.Empty;
    public string? OtherUserAvatarUrl { get; set; }
    public ChatConversationType ConversationType { get; set; }
    public int? ProductId { get; set; }
    public string? ProductName { get; set; }
    public int? ContactMessageId { get; set; }
    public int? ReportId { get; set; }
    public string? Subject { get; set; }
    public string LastMessagePreview { get; set; } = string.Empty;
    public DateTime LastMessageAt { get; set; }
    public int UnreadCount { get; set; }
    public bool IsAdminThread =>
        ConversationType == ChatConversationType.AdminContact
        || ConversationType == ChatConversationType.AdminReport;
}

public class ChatMessageDTO
{
    public int Id { get; set; }
    public int ConversationId { get; set; }
    public int SenderId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public string? SenderAvatarUrl { get; set; }
    public int RecipientId { get; set; }
    public string RecipientName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
    public bool IsAdminMessage { get; set; }
    public ChatConversationType ConversationType { get; set; }
    public int? ProductId { get; set; }
    public string? ProductName { get; set; }
    public string? Subject { get; set; }
}

public class StartConversationRequest
{
    [Required]
    public int RecipientId { get; set; }
    public int? ProductId { get; set; }
}

public class SendChatMessageRequest
{
    public int? ConversationId { get; set; }
    public int? RecipientId { get; set; }
    public int? ProductId { get; set; }

    [Required(ErrorMessage = "Treść wiadomości jest wymagana.")]
    [StringLength(2000, MinimumLength = 1, ErrorMessage = "Wiadomość musi mieć od 1 do 2000 znaków.")]
    public string Content { get; set; } = string.Empty;
}

public class AdminContactReplyRequest
{
    [Required(ErrorMessage = "Treść odpowiedzi jest wymagana.")]
    [StringLength(2000, MinimumLength = 1, ErrorMessage = "Odpowiedź musi mieć od 1 do 2000 znaków.")]
    public string Message { get; set; } = string.Empty;

    public bool SendEmailAlso { get; set; }
}

public class AdminReportReplyRequest
{
    public AdminReportReplyTarget Target { get; set; } = AdminReportReplyTarget.Reporter;

    [Required(ErrorMessage = "Treść wiadomości jest wymagana.")]
    [StringLength(2000, MinimumLength = 1, ErrorMessage = "Wiadomość musi mieć od 1 do 2000 znaków.")]
    public string Message { get; set; } = string.Empty;
}

public class AdminReplyResultDTO
{
    public bool Success { get; set; }
    public bool SentInApp { get; set; }
    public bool SentEmail { get; set; }
    public string? EmailWarning { get; set; }
    public string? ErrorMessage { get; set; }
    public int? ConversationId { get; set; }
    public ChatMessageDTO? Message { get; set; }
}

public class AdminReportThreadsDTO
{
    public int ReportId { get; set; }
    public int ReporterUserId { get; set; }
    public string ReporterUserName { get; set; } = string.Empty;
    public int ProductOwnerUserId { get; set; }
    public string ProductOwnerUserName { get; set; } = string.Empty;
    public List<ChatMessageDTO> Messages { get; set; } = new();
}

public class ChatUserLookupDTO
{
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? City { get; set; }
}

public class EncryptedMessageLogDTO
{
    public int Id { get; set; }
    public int ConversationId { get; set; }
    public int? MessageId { get; set; }
    public int SenderId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public int RecipientId { get; set; }
    public string RecipientName { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string EncryptedPayload { get; set; } = string.Empty;
    public string PayloadSha256 { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class DecryptedMessageLogDTO
{
    public int Id { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public string RecipientName { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string PlaintextContent { get; set; } = string.Empty;
    public DateTime TimestampUtc { get; set; }
    public bool HashValid { get; set; }
    public string? DecryptionError { get; set; }
}

public class EncryptedMessageLogPageDTO
{
    public List<EncryptedMessageLogDTO> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
