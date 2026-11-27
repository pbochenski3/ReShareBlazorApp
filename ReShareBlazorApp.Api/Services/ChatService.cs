using Microsoft.EntityFrameworkCore;
using ReShareBlazorApp.Api.Date;
using ReShareBlazorApp.Shared.DTOs;

namespace ReShareBlazorApp.Api.Services;

public sealed class ChatService
{
    private readonly ReshareDbContext _db;
    private readonly ChatEncryptionService _crypto;

    public ChatService(ReshareDbContext db, ChatEncryptionService crypto)
    { _db = db; _crypto = crypto; }

    public Task<bool> IsParticipantAsync(int conversationId, int userId, CancellationToken ct = default) =>
        _db.ChatParticipants.AnyAsync(x => x.ConversationId == conversationId && x.UserId == userId, ct);

    public async Task<int> GetOrCreateConversationAsync(int userA, int userB, bool support = false,
        string? reference = null, CancellationToken ct = default)
    {
        if (userA == userB) throw new InvalidOperationException("Nie można utworzyć rozmowy z samym sobą.");
        var existing = await _db.ChatConversations
            .Where(c => c.Participants.Count == 2 &&
                        c.Participants.Any(p => p.UserId == userA) &&
                        c.Participants.Any(p => p.UserId == userB) &&
                        c.IsSupport == support &&
                        (reference == null || c.SupportReference == reference))
            .Select(c => (int?)c.Id).FirstOrDefaultAsync(ct);
        if (existing.HasValue) return existing.Value;

        var users = await _db.Users.Where(u => (u.Id == userA || u.Id == userB) &&
                                               u.IsActive && !u.IsDeleted).Select(u => u.Id).ToListAsync(ct);
        if (users.Count != 2) throw new InvalidOperationException("Nie znaleziono aktywnych uczestników rozmowy.");

        var conversation = new ChatConversation
        {
            IsSupport = support, SupportReference = reference,
            Participants = new List<ChatParticipant>
            {
                new() { UserId = userA }, new() { UserId = userB }
            }
        };
        _db.ChatConversations.Add(conversation);
        await _db.SaveChangesAsync(ct);
        return conversation.Id;
    }

    public async Task<List<ChatConversationDto>> GetConversationsAsync(int userId, CancellationToken ct = default)
    {
        var rows = await _db.ChatConversations
            .Where(c => c.Participants.Any(p => p.UserId == userId))
            .Select(c => new
            {
                c.Id, c.IsSupport, LastMessageAt = c.LastMessageAt,
                Other = c.Participants.Where(p => p.UserId != userId)
                    .Select(p => new { p.UserId, p.User.UserName, p.User.AvatarPath }).FirstOrDefault(),
                Last = c.Messages.OrderByDescending(m => m.CreatedAt).Select(m => new
                { m.CipherText, m.Nonce, m.AuthTag }).FirstOrDefault(),
                LastRead = c.Participants.Where(p => p.UserId == userId)
                    .Select(p => p.LastReadAt).FirstOrDefault()
            })
            .OrderByDescending(x => x.LastMessageAt).ToListAsync(ct);

        var unreadByConversation = new Dictionary<int, int>();
        foreach (var row in rows)
        {
            var participant = await _db.ChatParticipants
                .Where(p => p.ConversationId == row.Id && p.UserId == userId)
                .Select(p => p.LastReadAt).SingleAsync(ct);
            unreadByConversation[row.Id] = await _db.ChatMessages.CountAsync(m =>
                m.ConversationId == row.Id && m.SenderId != userId &&
                (!participant.HasValue || m.CreatedAt > participant.Value), ct);
        }

        return rows.Select(x => new ChatConversationDto
        {
            Id = x.Id, IsSupport = x.IsSupport,
            OtherUserId = x.Other?.UserId ?? 0,
            OtherUserName = x.Other?.UserName ?? "Użytkownik",
            OtherUserAvatar = x.Other?.AvatarPath,
            LastMessageAt = x.LastMessageAt,
            LastMessage = x.Last == null ? null : SafeDecrypt(x.Last.CipherText, x.Last.Nonce, x.Last.AuthTag),
            UnreadCount = unreadByConversation.GetValueOrDefault(x.Id)
        }).ToList();
    }

    public async Task<List<ChatMessageDto>> GetMessagesAsync(int conversationId, int userId, CancellationToken ct = default)
    {
        if (!await IsParticipantAsync(conversationId, userId, ct)) throw new UnauthorizedAccessException();
        var rows = await _db.ChatMessages.Where(m => m.ConversationId == conversationId)
            .OrderBy(m => m.CreatedAt)
            .Select(m => new { m.Id, m.ConversationId, m.SenderId, SenderName = m.Sender.UserName,
                m.CipherText, m.Nonce, m.AuthTag, m.ReplyToMessageId, m.CreatedAt,
                Reply = m.ReplyToMessage == null ? null : new { m.ReplyToMessage.CipherText,
                    m.ReplyToMessage.Nonce, m.ReplyToMessage.AuthTag } })
            .ToListAsync(ct);
        return rows.Select(m => new ChatMessageDto
        {
            Id = m.Id, ConversationId = m.ConversationId, SenderId = m.SenderId,
            SenderName = m.SenderName ?? "Użytkownik",
            Text = _crypto.Decrypt(m.CipherText, m.Nonce, m.AuthTag),
            ReplyToMessageId = m.ReplyToMessageId,
            ReplyToText = m.Reply == null ? null : _crypto.Decrypt(m.Reply.CipherText, m.Reply.Nonce, m.Reply.AuthTag),
            CreatedAt = m.CreatedAt
        }).ToList();
    }

    public async Task<ChatMessageDto> SendAsync(int conversationId, int senderId, string text,
        int? replyToId, CancellationToken ct = default)
    {
        text = text.Trim();
        if (text.Length == 0 || text.Length > 4000) throw new ArgumentException("Wiadomość musi mieć od 1 do 4000 znaków.");
        if (!await IsParticipantAsync(conversationId, senderId, ct)) throw new UnauthorizedAccessException();
        if (replyToId.HasValue && !await _db.ChatMessages.AnyAsync(m =>
            m.Id == replyToId && m.ConversationId == conversationId, ct))
            throw new ArgumentException("Wiadomość, na którą odpowiadasz, nie istnieje w tej rozmowie.");

        var encrypted = _crypto.Encrypt(text);
        var message = new ChatMessage
        {
            ConversationId = conversationId, SenderId = senderId,
            CipherText = encrypted.CipherText, Nonce = encrypted.Nonce, AuthTag = encrypted.Tag,
            ReplyToMessageId = replyToId, CreatedAt = DateTime.UtcNow
        };
        _db.ChatMessages.Add(message);
        var conversation = await _db.ChatConversations.FindAsync(new object[] { conversationId }, ct);
        conversation!.LastMessageAt = message.CreatedAt;

        var audit = _crypto.Encrypt(System.Text.Json.JsonSerializer.Serialize(new
        { text, senderId, message.CreatedAt, replyToId }));
        _db.ChatAuditLogs.Add(new ChatAuditLog
        {
            ConversationId = conversationId, ActorUserId = senderId, EventType = "message.sent",
            CipherText = audit.CipherText, Nonce = audit.Nonce, AuthTag = audit.Tag,
            CreatedAt = message.CreatedAt
        });
        await _db.SaveChangesAsync(ct);

        var senderName = await _db.Users.Where(u => u.Id == senderId).Select(u => u.UserName).FirstAsync(ct);
        string? replyText = null;
        if (replyToId.HasValue)
        {
            var reply = await _db.ChatMessages.FindAsync(new object[] { replyToId.Value }, ct);
            if (reply != null) replyText = _crypto.Decrypt(reply.CipherText, reply.Nonce, reply.AuthTag);
        }
        return new ChatMessageDto
        {
            Id = message.Id, ConversationId = conversationId, SenderId = senderId,
            SenderName = senderName ?? "Użytkownik", Text = text, ReplyToMessageId = replyToId,
            ReplyToText = replyText, CreatedAt = message.CreatedAt
        };
    }

    public async Task MarkReadAsync(int conversationId, int userId, CancellationToken ct = default)
    {
        var participant = await _db.ChatParticipants.SingleOrDefaultAsync(
            p => p.ConversationId == conversationId && p.UserId == userId, ct);
        if (participant == null) throw new UnauthorizedAccessException();
        participant.LastReadAt = DateTime.UtcNow;
        var audit = _crypto.Encrypt(System.Text.Json.JsonSerializer.Serialize(new { userId, readAt = participant.LastReadAt }));
        _db.ChatAuditLogs.Add(new ChatAuditLog
        {
            ConversationId = conversationId, ActorUserId = userId, EventType = "conversation.read",
            CipherText = audit.CipherText, Nonce = audit.Nonce, AuthTag = audit.Tag
        });
        await _db.SaveChangesAsync(ct);
    }

    private string SafeDecrypt(string cipher, string nonce, string tag)
    {
        try { return _crypto.Decrypt(cipher, nonce, tag); }
        catch { return ""; }
    }
}
