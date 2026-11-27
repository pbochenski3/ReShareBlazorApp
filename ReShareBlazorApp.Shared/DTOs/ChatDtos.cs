using System.ComponentModel.DataAnnotations;

namespace ReShareBlazorApp.Shared.DTOs;

public sealed class CreateConversationRequest
{
    [Range(1, int.MaxValue)] public int OtherUserId { get; set; }
}

public sealed class SendChatMessageRequest
{
    [Required, StringLength(4000, MinimumLength = 1)]
    public string Text { get; set; } = "";
    public int? ReplyToMessageId { get; set; }
}

public sealed class ChatConversationDto
{
    public int Id { get; set; }
    public int OtherUserId { get; set; }
    public string OtherUserName { get; set; } = "";
    public string? OtherUserAvatar { get; set; }
    public string? LastMessage { get; set; }
    public DateTime? LastMessageAt { get; set; }
    public int UnreadCount { get; set; }
    public bool IsSupport { get; set; }
}

public sealed class ChatMessageDto
{
    public int Id { get; set; }
    public int ConversationId { get; set; }
    public int SenderId { get; set; }
    public string SenderName { get; set; } = "";
    public string Text { get; set; } = "";
    public int? ReplyToMessageId { get; set; }
    public string? ReplyToText { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class ChatMessagePushedDto
{
    public int ConversationId { get; set; }
    public ChatMessageDto Message { get; set; } = new();
}
