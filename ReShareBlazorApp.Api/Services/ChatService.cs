using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ReShareBlazorApp.Api.Date;
using ReShareBlazorApp.Api.Hubs;
using ReShareBlazorApp.Api.Interfaces.Services;
using ReShareBlazorApp.Shared;
using ReShareBlazorApp.Shared.DTOs;
using ReShareBlazorApp.Shared.Enums;

namespace ReShareBlazorApp.Api.Services;

public class ChatService : IChatService
{
    private readonly ReshareDbContext _context;
    private readonly IMessageEncryptionService _encryption;
    private readonly IHubContext<ChatHub> _hubContext;
    private readonly INotificationService _notificationService;
    private readonly IActivityLogger _activityLogger;
    private readonly IAppEmailSender _emailSender;
    private readonly ILogger<ChatService> _logger;

    public ChatService(
        ReshareDbContext context,
        IMessageEncryptionService encryption,
        IHubContext<ChatHub> hubContext,
        INotificationService notificationService,
        IActivityLogger activityLogger,
        IAppEmailSender emailSender,
        ILogger<ChatService> logger)
    {
        _context = context;
        _encryption = encryption;
        _hubContext = hubContext;
        _notificationService = notificationService;
        _activityLogger = activityLogger;
        _emailSender = emailSender;
        _logger = logger;
    }

    public async Task<List<ChatConversationDTO>> GetConversationsAsync(int currentUserId)
    {
        var conversations = await _context.ChatConversations
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Include(c => c.User1)
            .Include(c => c.User2)
            .Include(c => c.Product)
            .Where(c => c.User1Id == currentUserId || c.User2Id == currentUserId)
            .OrderByDescending(c => c.LastMessageAt)
            .ToListAsync();

        if (conversations.Count == 0)
            return new List<ChatConversationDTO>();

        var convIds = conversations.Select(c => c.Id).ToList();

        var unreadCounts = await _context.ChatMessages
            .AsNoTracking()
            .Where(m => convIds.Contains(m.ConversationId)
                && m.RecipientId == currentUserId
                && !m.IsRead)
            .GroupBy(m => m.ConversationId)
            .Select(g => new { ConversationId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ConversationId, x => x.Count);

        var lastMessages = await _context.ChatMessages
            .AsNoTracking()
            .Where(m => convIds.Contains(m.ConversationId))
            .GroupBy(m => m.ConversationId)
            .Select(g => g.OrderByDescending(m => m.SentAt).ThenByDescending(m => m.Id).First())
            .ToDictionaryAsync(m => m.ConversationId);

        var result = new List<ChatConversationDTO>(conversations.Count);
        foreach (var conv in conversations)
        {
            var otherUser = conv.User1Id == currentUserId ? conv.User2 : conv.User1;
            var preview = string.Empty;
            if (lastMessages.TryGetValue(conv.Id, out var lastMsg))
            {
                var plain = TryDecrypt(lastMsg.EncryptedContent);
                preview = lastMsg.SenderId == currentUserId ? $"Ty: {plain}" : plain;
                if (preview.Length > 80)
                    preview = preview[..80] + "…";
            }

            result.Add(new ChatConversationDTO
            {
                Id = conv.Id,
                OtherUserId = otherUser.Id,
                OtherUserName = otherUser.IsDeleted ? "(konto usunięte)" : (otherUser.UserName ?? "Użytkownik"),
                OtherUserAvatarUrl = string.IsNullOrEmpty(otherUser.AvatarPath) ? null : $"/avatars/{otherUser.AvatarPath}",
                ConversationType = conv.ConversationType,
                ProductId = conv.ProductId,
                ProductName = conv.Product?.Name,
                ContactMessageId = conv.ContactMessageId,
                ReportId = conv.ReportId,
                Subject = conv.Subject,
                LastMessagePreview = preview,
                LastMessageAt = conv.LastMessageAt,
                UnreadCount = unreadCounts.GetValueOrDefault(conv.Id, 0)
            });
        }

        return result;
    }

    public async Task<(ChatConversationDTO? Conversation, string? Error)> GetOrCreateConversationAsync(
        int currentUserId, int recipientId, int? productId)
    {
        if (recipientId == currentUserId)
            return (null, "Nie możesz rozpocząć rozmowy z samym sobą.");

        var recipient = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == recipientId && !u.IsDeleted);

        if (recipient is null)
            return (null, "Nie znaleziono odbiorcy.");

        Product? product = null;
        if (productId.HasValue && productId.Value > 0)
        {
            product = await _context.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == productId.Value && !p.isDeleted);

            if (product is null)
                return (null, "Nie znaleziono ogłoszenia.");
        }

        var (u1, u2) = OrderUsers(currentUserId, recipientId);
        var convType = product is not null ? ChatConversationType.Product : ChatConversationType.Direct;
        int? resolvedProductId = product?.Id;

        var existing = await _context.ChatConversations
            .Include(c => c.User1)
            .Include(c => c.User2)
            .Include(c => c.Product)
            .FirstOrDefaultAsync(c =>
                c.User1Id == u1
                && c.User2Id == u2
                && c.ConversationType == convType
                && c.ProductId == resolvedProductId);

        if (existing is null)
        {
            existing = new ChatConversation
            {
                User1Id = u1,
                User2Id = u2,
                ConversationType = convType,
                ProductId = resolvedProductId,
                Subject = product?.Name,
                CreatedAt = DateTime.UtcNow,
                LastMessageAt = DateTime.UtcNow
            };
            _context.ChatConversations.Add(existing);
            await _context.SaveChangesAsync();

            existing = await _context.ChatConversations
                .AsNoTracking()
                .Include(c => c.User1)
                .Include(c => c.User2)
                .Include(c => c.Product)
                .FirstAsync(c => c.Id == existing.Id);
        }

        var otherUser = existing.User1Id == currentUserId ? existing.User2 : existing.User1;
        return (new ChatConversationDTO
        {
            Id = existing.Id,
            OtherUserId = otherUser.Id,
            OtherUserName = otherUser.UserName ?? "Użytkownik",
            OtherUserAvatarUrl = string.IsNullOrEmpty(otherUser.AvatarPath) ? null : $"/avatars/{otherUser.AvatarPath}",
            ConversationType = existing.ConversationType,
            ProductId = existing.ProductId,
            ProductName = existing.Product?.Name,
            Subject = existing.Subject,
            LastMessagePreview = string.Empty,
            LastMessageAt = existing.LastMessageAt,
            UnreadCount = 0
        }, null);
    }

    public async Task<(List<ChatMessageDTO>? Messages, string? Error)> GetMessagesAsync(int currentUserId, int conversationId)
    {
        var conversation = await _context.ChatConversations
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Include(c => c.User1)
            .Include(c => c.User2)
            .Include(c => c.Product)
            .FirstOrDefaultAsync(c => c.Id == conversationId);

        if (conversation is null)
            return (null, "Nie znaleziono konwersacji.");

        if (conversation.User1Id != currentUserId && conversation.User2Id != currentUserId)
            return (null, "Brak dostępu do tej konwersacji.");

        await MarkConversationAsReadAsync(currentUserId, conversationId);

        var messages = await _context.ChatMessages
            .AsNoTracking()
            .Where(m => m.ConversationId == conversationId)
            .OrderBy(m => m.SentAt)
            .ThenBy(m => m.Id)
            .ToListAsync();

        var dtos = messages.Select(m => MapMessageDto(m, conversation)).ToList();
        return (dtos, null);
    }

    public async Task<(ChatMessageDTO? Message, string? Error)> SendMessageAsync(
        int senderId, SendChatMessageRequest request, bool isAdminMessage = false)
    {
        var content = request.Content?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(content))
            return (null, "Wiadomość nie może być pusta.");

        if (content.Length > 2000)
            return (null, "Wiadomość może mieć maksymalnie 2000 znaków.");

        ChatConversation? conversation = null;

        if (request.ConversationId.HasValue && request.ConversationId.Value > 0)
        {
            conversation = await _context.ChatConversations
                .IgnoreQueryFilters()
                .Include(c => c.User1)
                .Include(c => c.User2)
                .Include(c => c.Product)
                .FirstOrDefaultAsync(c => c.Id == request.ConversationId.Value);

            if (conversation is null)
                return (null, "Nie znaleziono konwersacji.");

            if (conversation.User1Id != senderId && conversation.User2Id != senderId)
                return (null, "Brak dostępu do tej konwersacji.");
        }
        else if (request.RecipientId.HasValue && request.RecipientId.Value > 0)
        {
            var (convDto, convErr) = await GetOrCreateConversationAsync(senderId, request.RecipientId.Value, request.ProductId);
            if (convErr is not null || convDto is null)
                return (null, convErr ?? "Nie udało się utworzyć konwersacji.");

            conversation = await _context.ChatConversations
                .IgnoreQueryFilters()
                .Include(c => c.User1)
                .Include(c => c.User2)
                .Include(c => c.Product)
                .FirstAsync(c => c.Id == convDto.Id);
        }
        else
        {
            return (null, "Wybierz konwersację lub odbiorcę wiadomości.");
        }

        var recipientId = conversation.User1Id == senderId ? conversation.User2Id : conversation.User1Id;
        var senderUser = conversation.User1Id == senderId ? conversation.User1 : conversation.User2;
        var recipientUser = conversation.User1Id == senderId ? conversation.User2 : conversation.User1;

        if (recipientUser.IsDeleted)
            return (null, "Nie można wysłać wiadomości do usuniętego konta.");

        var eventType = conversation.ConversationType switch
        {
            ChatConversationType.Product => "ProductMessage",
            ChatConversationType.AdminContact => "AdminContactReply",
            ChatConversationType.AdminReport => "AdminReportReply",
            _ => "DirectMessage"
        };

        var dto = await PersistAndBroadcastAsync(
            conversation,
            senderUser,
            recipientUser,
            content,
            isAdminMessage,
            eventType);

        return (dto, null);
    }

    public async Task<int> MarkConversationAsReadAsync(int currentUserId, int conversationId)
    {
        var unreadMessages = await _context.ChatMessages
            .Where(m => m.ConversationId == conversationId
                && m.RecipientId == currentUserId
                && !m.IsRead)
            .ToListAsync();

        if (unreadMessages.Count == 0)
            return 0;

        var now = DateTime.UtcNow;
        var senderIds = new HashSet<int>();
        foreach (var msg in unreadMessages)
        {
            msg.IsRead = true;
            msg.ReadAt = now;
            senderIds.Add(msg.SenderId);
        }

        await _context.SaveChangesAsync();

        foreach (var senderId in senderIds)
        {
            await _hubContext.Clients.User(senderId.ToString())
                .SendAsync("ConversationRead", conversationId, currentUserId);
        }

        await _hubContext.Clients.User(currentUserId.ToString())
            .SendAsync("UnreadCountChanged");

        return unreadMessages.Count;
    }

    public async Task<int> GetUnreadCountAsync(int currentUserId)
    {
        return await _context.ChatMessages
            .AsNoTracking()
            .CountAsync(m => m.RecipientId == currentUserId && !m.IsRead);
    }

    public async Task<List<ChatUserLookupDTO>> SearchUsersAsync(int currentUserId, string? query)
    {
        var q = query?.Trim() ?? string.Empty;
        var usersQuery = _context.Users
            .AsNoTracking()
            .Where(u => !u.IsDeleted && u.IsActive && u.Id != currentUserId);

        if (!string.IsNullOrWhiteSpace(q))
        {
            usersQuery = usersQuery.Where(u => u.UserName != null && u.UserName.Contains(q));
        }

        return await usersQuery
            .OrderBy(u => u.UserName)
            .Take(12)
            .Select(u => new ChatUserLookupDTO
            {
                UserId = u.Id,
                UserName = u.UserName ?? string.Empty,
                AvatarUrl = string.IsNullOrEmpty(u.AvatarPath) ? null : $"/avatars/{u.AvatarPath}",
                City = u.City
            })
            .ToListAsync();
    }

    public async Task<List<ChatMessageDTO>> GetContactThreadAsync(int contactMessageId)
    {
        var conversations = await _context.ChatConversations
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Include(c => c.User1)
            .Include(c => c.User2)
            .Include(c => c.Product)
            .Where(c => c.ContactMessageId == contactMessageId)
            .ToListAsync();

        if (conversations.Count == 0)
            return new List<ChatMessageDTO>();

        var convMap = conversations.ToDictionary(c => c.Id);
        var messages = await _context.ChatMessages
            .AsNoTracking()
            .Where(m => convMap.Keys.Contains(m.ConversationId))
            .OrderBy(m => m.SentAt)
            .ThenBy(m => m.Id)
            .ToListAsync();

        return messages.Select(m => MapMessageDto(m, convMap[m.ConversationId])).ToList();
    }

    public async Task<AdminReplyResultDTO> SendAdminContactReplyAsync(
        ClaimsPrincipal adminPrincipal, int adminUserId, int contactMessageId, AdminContactReplyRequest request)
    {
        var replyText = request.Message?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(replyText))
            return new AdminReplyResultDTO { Success = false, ErrorMessage = "Treść odpowiedzi jest wymagana." };

        var contactMsg = await _context.ContactMessages.FirstOrDefaultAsync(c => c.Id == contactMessageId);
        if (contactMsg is null)
            return new AdminReplyResultDTO { Success = false, ErrorMessage = "Nie znaleziono wiadomości kontaktowej." };

        var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == adminUserId && !u.IsDeleted);
        if (adminUser is null)
            return new AdminReplyResultDTO { Success = false, ErrorMessage = "Nie znaleziono konta administratora." };

        User? targetUser = null;
        if (contactMsg.UserId.HasValue)
        {
            targetUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == contactMsg.UserId.Value && !u.IsDeleted);
        }

        if (targetUser is null && !string.IsNullOrWhiteSpace(contactMsg.Email))
        {
            var normalizedEmail = contactMsg.Email.Trim().ToUpperInvariant();
            targetUser = await _context.Users.FirstOrDefaultAsync(u =>
                !u.IsDeleted && u.NormalizedEmail == normalizedEmail);
        }

        var result = new AdminReplyResultDTO();

        if (targetUser is not null && targetUser.Id != adminUserId)
        {
            var (u1, u2) = OrderUsers(adminUserId, targetUser.Id);
            var conversation = await _context.ChatConversations
                .IgnoreQueryFilters()
                .Include(c => c.User1)
                .Include(c => c.User2)
                .Include(c => c.Product)
                .FirstOrDefaultAsync(c =>
                    c.User1Id == u1
                    && c.User2Id == u2
                    && c.ConversationType == ChatConversationType.AdminContact
                    && c.ContactMessageId == contactMessageId);

            if (conversation is null)
            {
                conversation = new ChatConversation
                {
                    User1Id = u1,
                    User2Id = u2,
                    ConversationType = ChatConversationType.AdminContact,
                    ContactMessageId = contactMessageId,
                    Subject = $"Kontakt: {contactMsg.Subject}",
                    CreatedAt = DateTime.UtcNow,
                    LastMessageAt = DateTime.UtcNow
                };
                _context.ChatConversations.Add(conversation);
                await _context.SaveChangesAsync();

                conversation = await _context.ChatConversations
                    .IgnoreQueryFilters()
                    .Include(c => c.User1)
                    .Include(c => c.User2)
                    .Include(c => c.Product)
                    .FirstAsync(c => c.Id == conversation.Id);
            }

            var dto = await PersistAndBroadcastAsync(
                conversation,
                adminUser,
                targetUser,
                replyText,
                isAdminMessage: true,
                eventType: "AdminContactReply",
                customNotificationTitle: "Odpowiedź administracji",
                customNotificationBody: $"Odpowiedź na wiadomość „{contactMsg.Subject}”: {Truncate(replyText, 120)}");

            result.SentInApp = true;
            result.ConversationId = conversation.Id;
            result.Message = dto;
        }
        else if (!request.SendEmailAlso)
        {
            var reason = targetUser is null
                ? "Nadawca nie ma powiązanego konta w serwisie – zaznacz wysyłkę na e-mail, aby odpowiedzieć."
                : "Nie można wysłać wiadomości czatu do własnego konta administratora.";
            return new AdminReplyResultDTO { Success = false, ErrorMessage = reason };
        }

        if (request.SendEmailAlso && !string.IsNullOrWhiteSpace(contactMsg.Email))
        {
            try
            {
                var safeSubject = WebUtility.HtmlEncode(contactMsg.Subject);
                var safeName = WebUtility.HtmlEncode(contactMsg.Name);
                var safeReply = WebUtility.HtmlEncode(replyText).Replace("\n", "<br />");
                var htmlBody = $@"
                    <p>Cześć {safeName},</p>
                    <p>Odpowiedź zespołu <strong>ODDAJ DALEJ</strong> na Twoją wiadomość „{safeSubject}”:</p>
                    <blockquote style=""border-left:3px solid #0f766e;padding-left:12px;margin:12px 0;"">{safeReply}</blockquote>
                    <p>Możesz również kontynuować rozmowę w zakładce <strong>Wiadomości</strong> po zalogowaniu w serwisie.</p>";

                await _emailSender.SendAsync(contactMsg.Email, $"Re: {contactMsg.Subject} – ODDAJ DALEJ", htmlBody);
                result.SentEmail = true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Nie udało się wysłać e-maila z odpowiedzią na wiadomość kontaktową {Id}", contactMessageId);
                result.EmailWarning = "Odpowiedź zapisano w czacie aplikacji, ale wysyłka e-mail nie powiodła się.";
            }
        }

        contactMsg.IsRead = true;
        contactMsg.IsReplied = true;
        contactMsg.RepliedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await _activityLogger.LogAsync(
            adminPrincipal,
            ActivityActions.AdminContactReplied,
            $"Odpowiedziano na wiadomość kontaktową (id: {contactMessageId}, temat: {contactMsg.Subject})",
            "ContactMessage",
            contactMessageId);

        result.Success = result.SentInApp || result.SentEmail;
        if (!result.Success && string.IsNullOrEmpty(result.ErrorMessage))
            result.ErrorMessage = result.EmailWarning ?? "Nie udało się wysłać odpowiedzi.";

        return result;
    }

    public async Task<AdminReportThreadsDTO?> GetReportThreadsAsync(int reportId)
    {
        var report = await _context.Reports
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Include(r => r.User)
            .Include(r => r.Product)
                .ThenInclude(p => p.user)
            .FirstOrDefaultAsync(r => r.Id == reportId);

        if (report is null)
            return null;

        var conversations = await _context.ChatConversations
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Include(c => c.User1)
            .Include(c => c.User2)
            .Include(c => c.Product)
            .Where(c => c.ReportId == reportId)
            .ToListAsync();

        var messages = new List<ChatMessageDTO>();
        if (conversations.Count > 0)
        {
            var convMap = conversations.ToDictionary(c => c.Id);
            var rawMessages = await _context.ChatMessages
                .AsNoTracking()
                .Where(m => convMap.Keys.Contains(m.ConversationId))
                .OrderBy(m => m.SentAt)
                .ThenBy(m => m.Id)
                .ToListAsync();

            messages = rawMessages.Select(m => MapMessageDto(m, convMap[m.ConversationId])).ToList();
        }

        return new AdminReportThreadsDTO
        {
            ReportId = report.Id,
            ReporterUserId = report.UserId,
            ReporterUserName = report.User?.UserName ?? "Zgłaszający",
            ProductOwnerUserId = report.Product.UserId,
            ProductOwnerUserName = report.Product.user?.UserName ?? "Autor ogłoszenia",
            Messages = messages
        };
    }

    public async Task<AdminReplyResultDTO> SendAdminReportReplyAsync(
        ClaimsPrincipal adminPrincipal, int adminUserId, int reportId, AdminReportReplyRequest request)
    {
        var text = request.Message?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(text))
            return new AdminReplyResultDTO { Success = false, ErrorMessage = "Treść wiadomości jest wymagana." };

        var report = await _context.Reports
            .IgnoreQueryFilters()
            .Include(r => r.User)
            .Include(r => r.Product)
                .ThenInclude(p => p.user)
            .FirstOrDefaultAsync(r => r.Id == reportId);

        if (report is null)
            return new AdminReplyResultDTO { Success = false, ErrorMessage = "Nie znaleziono zgłoszenia." };

        var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == adminUserId && !u.IsDeleted);
        if (adminUser is null)
            return new AdminReplyResultDTO { Success = false, ErrorMessage = "Nie znaleziono konta administratora." };

        var targetUser = request.Target == AdminReportReplyTarget.ProductOwner
            ? report.Product.user
            : report.User;

        if (targetUser is null || targetUser.IsDeleted)
            return new AdminReplyResultDTO { Success = false, ErrorMessage = "Docelowy użytkownik nie istnieje lub usunął konto." };

        if (targetUser.Id == adminUserId)
            return new AdminReplyResultDTO { Success = false, ErrorMessage = "Nie można wysłać wiadomości czatu do własnego konta administratora." };

        var (u1, u2) = OrderUsers(adminUserId, targetUser.Id);
        var conversation = await _context.ChatConversations
            .IgnoreQueryFilters()
            .Include(c => c.User1)
            .Include(c => c.User2)
            .Include(c => c.Product)
            .FirstOrDefaultAsync(c =>
                c.User1Id == u1
                && c.User2Id == u2
                && c.ConversationType == ChatConversationType.AdminReport
                && c.ReportId == reportId);

        if (conversation is null)
        {
            conversation = new ChatConversation
            {
                User1Id = u1,
                User2Id = u2,
                ConversationType = ChatConversationType.AdminReport,
                ReportId = reportId,
                ProductId = report.ProductId,
                Subject = $"Zgłoszenie #{report.Id}: {Truncate(report.Product.Name, 140)}",
                CreatedAt = DateTime.UtcNow,
                LastMessageAt = DateTime.UtcNow
            };
            _context.ChatConversations.Add(conversation);
            await _context.SaveChangesAsync();

            conversation = await _context.ChatConversations
                .IgnoreQueryFilters()
                .Include(c => c.User1)
                .Include(c => c.User2)
                .Include(c => c.Product)
                .FirstAsync(c => c.Id == conversation.Id);
        }

        var dto = await PersistAndBroadcastAsync(
            conversation,
            adminUser,
            targetUser,
            text,
            isAdminMessage: true,
            eventType: "AdminReportReply",
            customNotificationTitle: "Wiadomość od administracji ws. zgłoszenia",
            customNotificationBody: $"Zgłoszenie #{report.Id} ({report.Product.Name}): {Truncate(text, 120)}");

        var targetLabel = request.Target == AdminReportReplyTarget.ProductOwner ? "autora ogłoszenia" : "zgłaszającego";
        await _activityLogger.LogAsync(
            adminPrincipal,
            ActivityActions.AdminReportReplied,
            $"Wysłano wiadomość do {targetLabel} ({targetUser.UserName}) w sprawie zgłoszenia #{reportId}",
            "Report",
            reportId);

        return new AdminReplyResultDTO
        {
            Success = true,
            SentInApp = true,
            ConversationId = conversation.Id,
            Message = dto
        };
    }

    public async Task<EncryptedMessageLogPageDTO> GetMessageLogsAsync(string? search, string? eventType, int page, int pageSize)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _context.EncryptedMessageLogs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(l =>
                l.SenderName.Contains(s)
                || l.RecipientName.Contains(s)
                || l.PayloadSha256.Contains(s));
        }

        if (!string.IsNullOrWhiteSpace(eventType))
        {
            var et = eventType.Trim();
            query = query.Where(l => l.EventType == et);
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(l => l.CreatedAt)
            .ThenByDescending(l => l.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new EncryptedMessageLogDTO
            {
                Id = l.Id,
                ConversationId = l.ConversationId,
                MessageId = l.MessageId,
                SenderId = l.SenderId,
                SenderName = l.SenderName,
                RecipientId = l.RecipientId,
                RecipientName = l.RecipientName,
                EventType = l.EventType,
                EncryptedPayload = l.EncryptedPayload,
                PayloadSha256 = l.PayloadSha256,
                CreatedAt = l.CreatedAt
            })
            .ToListAsync();

        return new EncryptedMessageLogPageDTO
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<DecryptedMessageLogDTO?> DecryptMessageLogAsync(ClaimsPrincipal adminPrincipal, int logId)
    {
        var log = await _context.EncryptedMessageLogs
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == logId);

        if (log is null)
            return null;

        try
        {
            var decryptedJson = _encryption.Decrypt(log.EncryptedPayload);
            var computedHash = _encryption.ComputeSha256(decryptedJson);
            var hashValid = string.Equals(computedHash, log.PayloadSha256, StringComparison.OrdinalIgnoreCase);

            var payload = JsonSerializer.Deserialize<MessageLogPayload>(decryptedJson);

            await _activityLogger.LogAsync(
                adminPrincipal,
                ActivityActions.AdminMessageLogDecrypted,
                $"Odszyfrowano log wiadomości (id: {log.Id}, konwersacja: {log.ConversationId})",
                "EncryptedMessageLog",
                log.Id);

            return new DecryptedMessageLogDTO
            {
                Id = log.Id,
                SenderName = payload?.SenderName ?? log.SenderName,
                RecipientName = payload?.RecipientName ?? log.RecipientName,
                EventType = payload?.EventType ?? log.EventType,
                PlaintextContent = payload?.Content ?? string.Empty,
                TimestampUtc = payload?.TimestampUtc ?? log.CreatedAt,
                HashValid = hashValid
            };
        }
        catch (CryptographicException ex)
        {
            _logger.LogError(ex, "Błąd deszyfrowania logu wiadomości {LogId}", logId);
            return new DecryptedMessageLogDTO
            {
                Id = log.Id,
                SenderName = log.SenderName,
                RecipientName = log.RecipientName,
                EventType = log.EventType,
                TimestampUtc = log.CreatedAt,
                HashValid = false,
                DecryptionError = "Nie udało się odszyfrować wpisu (błędny klucz lub uszkodzony szyfrogram)."
            };
        }
    }

    private async Task<ChatMessageDTO> PersistAndBroadcastAsync(
        ChatConversation conversation,
        User sender,
        User recipient,
        string plaintextContent,
        bool isAdminMessage,
        string eventType,
        string? customNotificationTitle = null,
        string? customNotificationBody = null)
    {
        var now = DateTime.UtcNow;
        var encryptedContent = _encryption.Encrypt(plaintextContent);
        var contentHash = _encryption.ComputeSha256(plaintextContent);

        var entity = new ChatMessage
        {
            ConversationId = conversation.Id,
            SenderId = sender.Id,
            RecipientId = recipient.Id,
            EncryptedContent = encryptedContent,
            ContentHash = contentHash,
            SentAt = now,
            IsRead = false,
            IsAdminMessage = isAdminMessage
        };

        _context.ChatMessages.Add(entity);
        conversation.LastMessageAt = now;
        await _context.SaveChangesAsync();

        // Zapisujemy zaszyfrowany log wiadomości
        var logPayloadObj = new MessageLogPayload
        {
            ConversationId = conversation.Id,
            MessageId = entity.Id,
            SenderId = sender.Id,
            SenderName = sender.UserName ?? "Użytkownik",
            RecipientId = recipient.Id,
            RecipientName = recipient.UserName ?? "Użytkownik",
            EventType = eventType,
            Content = plaintextContent,
            TimestampUtc = now
        };
        var logJson = JsonSerializer.Serialize(logPayloadObj);
        var encryptedLogPayload = _encryption.Encrypt(logJson);
        var logSha256 = _encryption.ComputeSha256(logJson);

        _context.EncryptedMessageLogs.Add(new EncryptedMessageLog
        {
            ConversationId = conversation.Id,
            MessageId = entity.Id,
            SenderId = sender.Id,
            SenderName = logPayloadObj.SenderName,
            RecipientId = recipient.Id,
            RecipientName = logPayloadObj.RecipientName,
            EventType = eventType,
            EncryptedPayload = encryptedLogPayload,
            PayloadSha256 = logSha256,
            CreatedAt = now
        });
        await _context.SaveChangesAsync();

        // Powiadomienie w systemie powiadomień
        try
        {
            var notifTitle = customNotificationTitle ?? $"Nowa wiadomość od {logPayloadObj.SenderName}";
            var notifBody = customNotificationBody ?? Truncate(plaintextContent, 140);
            await _notificationService.CreateAsync(
                recipient.Id,
                NotificationType.NewMessage,
                notifTitle,
                notifBody,
                productId: conversation.ProductId,
                actorName: logPayloadObj.SenderName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Nie udało się zapisać powiadomienia dla wiadomości {MessageId}", entity.Id);
        }

        var dto = new ChatMessageDTO
        {
            Id = entity.Id,
            ConversationId = conversation.Id,
            SenderId = sender.Id,
            SenderName = sender.IsDeleted ? "(konto usunięte)" : (sender.UserName ?? "Użytkownik"),
            SenderAvatarUrl = string.IsNullOrEmpty(sender.AvatarPath) ? null : $"/avatars/{sender.AvatarPath}",
            RecipientId = recipient.Id,
            RecipientName = recipient.IsDeleted ? "(konto usunięte)" : (recipient.UserName ?? "Użytkownik"),
            Content = plaintextContent,
            SentAt = entity.SentAt,
            IsRead = entity.IsRead,
            ReadAt = entity.ReadAt,
            IsAdminMessage = entity.IsAdminMessage,
            ConversationType = conversation.ConversationType,
            ProductId = conversation.ProductId,
            ProductName = conversation.Product?.Name,
            Subject = conversation.Subject
        };

        await _hubContext.Clients.User(recipient.Id.ToString())
            .SendAsync("ReceiveMessage", dto);
        await _hubContext.Clients.User(sender.Id.ToString())
            .SendAsync("ReceiveMessage", dto);
        await _hubContext.Clients.User(recipient.Id.ToString())
            .SendAsync("UnreadCountChanged");

        return dto;
    }

    private ChatMessageDTO MapMessageDto(ChatMessage m, ChatConversation conv)
    {
        var sender = conv.User1Id == m.SenderId ? conv.User1 : conv.User2;
        var recipient = conv.User1Id == m.RecipientId ? conv.User1 : conv.User2;
        return new ChatMessageDTO
        {
            Id = m.Id,
            ConversationId = m.ConversationId,
            SenderId = m.SenderId,
            SenderName = sender.IsDeleted ? "(konto usunięte)" : (sender.UserName ?? "Użytkownik"),
            SenderAvatarUrl = string.IsNullOrEmpty(sender.AvatarPath) ? null : $"/avatars/{sender.AvatarPath}",
            RecipientId = m.RecipientId,
            RecipientName = recipient.IsDeleted ? "(konto usunięte)" : (recipient.UserName ?? "Użytkownik"),
            Content = TryDecrypt(m.EncryptedContent),
            SentAt = m.SentAt,
            IsRead = m.IsRead,
            ReadAt = m.ReadAt,
            IsAdminMessage = m.IsAdminMessage,
            ConversationType = conv.ConversationType,
            ProductId = conv.ProductId,
            ProductName = conv.Product?.Name,
            Subject = conv.Subject
        };
    }

    private string TryDecrypt(string encryptedContent)
    {
        try
        {
            return _encryption.Decrypt(encryptedContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd deszyfrowania wiadomości czatu.");
            return "[Nie udało się odszyfrować wiadomości]";
        }
    }

    private static (int u1, int u2) OrderUsers(int a, int b) => a < b ? (a, b) : (b, a);

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength] + "…";

    private sealed class MessageLogPayload
    {
        public int ConversationId { get; set; }
        public int MessageId { get; set; }
        public int SenderId { get; set; }
        public string SenderName { get; set; } = string.Empty;
        public int RecipientId { get; set; }
        public string RecipientName { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime TimestampUtc { get; set; }
    }
}
