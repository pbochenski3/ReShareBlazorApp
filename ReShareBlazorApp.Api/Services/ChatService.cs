using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using ReShareBlazorApp.Api.Date;
using ReShareBlazorApp.Api.Hubs;
using ReShareBlazorApp.Api.Interfaces.Services;
using ReShareBlazorApp.Shared.DTOs;
using ReShareBlazorApp.Shared.Enums;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace ReShareBlazorApp.Api.Services
{
    public class ChatService : IChatService
    {
        private const int RateLimitMessages = 15;
        private const int PreviewLength = 80;
        private static readonly TimeSpan RateLimitWindow = TimeSpan.FromSeconds(10);

        private readonly ReshareDbContext _context;
        private readonly IMessageEncryptionService _encryption;
        private readonly INotificationService _notificationService;
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly IMemoryCache _cache;
        private readonly ILogger<ChatService> _logger;

        public ChatService(
            ReshareDbContext context,
            IMessageEncryptionService encryption,
            INotificationService notificationService,
            IHubContext<ChatHub> hubContext,
            IMemoryCache cache,
            ILogger<ChatService> logger)
        {
            _context = context;
            _encryption = encryption;
            _notificationService = notificationService;
            _hubContext = hubContext;
            _cache = cache;
            _logger = logger;
        }

        // ---------- Rozpoczynanie rozmów ----------

        public async Task<ConversationDTO> StartConversationAsync(int currentUserId, int recipientId, int? productId)
        {
            if (currentUserId == recipientId)
                throw new ChatException("Nie możesz pisać do samego siebie.");

            var activeUserIds = await _context.Users
                .AsNoTracking()
                .Where(u => (u.Id == currentUserId || u.Id == recipientId) && u.IsActive)
                .Select(u => u.Id)
                .ToListAsync();

            if (activeUserIds.Count != 2)
                throw new ChatException("Nie można rozpocząć rozmowy z tym użytkownikiem.");

            int? validProductId = null;
            if (productId.HasValue && productId.Value > 0)
            {
                var pid = productId.Value;
                var productOk = await _context.Products
                    .AsNoTracking()
                    .AnyAsync(p => p.Id == pid && (p.UserId == recipientId || p.UserId == currentUserId));
                if (productOk)
                    validProductId = pid;
            }

            var low = Math.Min(currentUserId, recipientId);
            var high = Math.Max(currentUserId, recipientId);

            var conversation = await FindDirectAsync(low, high, validProductId);
            if (conversation is null)
            {
                conversation = new Conversation
                {
                    Type = ConversationType.Direct,
                    UserLowId = low,
                    UserHighId = high,
                    ProductId = validProductId
                };
                _context.Conversations.Add(conversation);

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    _context.Entry(conversation).State = EntityState.Detached;
                    conversation = await FindDirectAsync(low, high, validProductId);
                    if (conversation is null)
                        throw;
                }
            }

            var dtos = await BuildConversationDtosAsync(new List<Conversation> { conversation }, currentUserId, false);
            return dtos[0];
        }

        private Task<Conversation?> FindDirectAsync(int low, int high, int? productId) =>
            _context.Conversations
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Type == ConversationType.Direct
                    && c.UserLowId == low
                    && c.UserHighId == high
                    && c.ProductId == productId);

        private Task<Conversation?> FindSupportAsync(int customerId) =>
            _context.Conversations
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Type == ConversationType.Support && c.UserLowId == customerId);

        private async Task<Conversation> GetOrCreateSupportConversationAsync(int customerId)
        {
            var existing = await FindSupportAsync(customerId);
            if (existing is not null)
                return existing;

            var conversation = new Conversation
            {
                Type = ConversationType.Support,
                UserLowId = customerId,
                UserHighId = null,
                ProductId = null
            };
            _context.Conversations.Add(conversation);

            try
            {
                await _context.SaveChangesAsync();
                return conversation;
            }
            catch (DbUpdateException)
            {
                _context.Entry(conversation).State = EntityState.Detached;
                var created = await FindSupportAsync(customerId);
                if (created is null)
                    throw;
                return created;
            }
        }

        // ---------- Odczyt ----------

        public async Task<List<ConversationDTO>> GetConversationsAsync(int userId)
        {
            var conversations = await _context.Conversations
                .AsNoTracking()
                .Where(c =>
                    ((c.Type == ConversationType.Direct && (c.UserLowId == userId || c.UserHighId == userId))
                        || (c.Type == ConversationType.Support && c.UserLowId == userId))
                    && _context.ChatMessages.Any(m => m.ConversationId == c.Id))
                .OrderByDescending(c => c.LastMessageAt)
                .Take(200)
                .ToListAsync();

            return await BuildConversationDtosAsync(conversations, userId, false);
        }

        public async Task<List<ConversationDTO>> GetSupportConversationsAsync()
        {
            var conversations = await _context.Conversations
                .AsNoTracking()
                .Where(c => c.Type == ConversationType.Support
                    && _context.ChatMessages.Any(m => m.ConversationId == c.Id))
                .OrderByDescending(c => c.LastMessageAt)
                .Take(200)
                .ToListAsync();

            return await BuildConversationDtosAsync(conversations, 0, true);
        }

        public async Task<ConversationDTO?> GetConversationAsync(int userId, bool isAdmin, int conversationId)
        {
            var conversation = await _context.Conversations
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == conversationId);

            if (conversation is null || !CanAccess(conversation, userId, isAdmin))
                return null;

            var dtos = await BuildConversationDtosAsync(
                new List<Conversation> { conversation },
                userId,
                IsAdminView(conversation, userId, isAdmin));

            return dtos[0];
        }

        public async Task<List<ChatMessageDTO>?> GetMessagesAsync(
            int userId, bool isAdmin, int conversationId, int? beforeId, int take)
        {
            var conversation = await _context.Conversations
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == conversationId);

            if (conversation is null || !CanAccess(conversation, userId, isAdmin))
                return null;

            take = Math.Clamp(take, 1, 100);

            IQueryable<ChatMessage> query = _context.ChatMessages
                .AsNoTracking()
                .Where(m => m.ConversationId == conversationId);

            if (beforeId.HasValue)
            {
                var before = beforeId.Value;
                query = query.Where(m => m.Id < before);
            }

            var page = await query
                .OrderByDescending(m => m.Id)
                .Take(take)
                .ToListAsync();

            page.Reverse();
            return await ToMessageDtosAsync(conversation, page);
        }

        // ---------- Wysyłanie ----------

        public async Task<ChatMessageDTO> SendMessageAsync(
            int senderId, bool senderIsAdmin, int conversationId, string content, string? ipAddress)
        {
            content = (content ?? string.Empty).Trim();
            if (content.Length == 0)
                throw new ChatException("Wiadomość nie może być pusta.");
            if (content.Length > ChatConstants.MaxMessageLength)
                throw new ChatException($"Wiadomość może mieć maksymalnie {ChatConstants.MaxMessageLength} znaków.");

            EnforceRateLimit(senderId);

            var sender = await _context.Users
                .AsNoTracking()
                .Where(u => u.Id == senderId)
                .Select(u => new { u.UserName, u.IsActive, u.LockoutEnd })
                .FirstOrDefaultAsync();

            if (sender is null
                || !sender.IsActive
                || (sender.LockoutEnd.HasValue && sender.LockoutEnd.Value > DateTimeOffset.UtcNow))
            {
                throw new ChatException("Twoje konto nie może teraz wysyłać wiadomości.");
            }

            var conversation = await _context.Conversations.FirstOrDefaultAsync(c => c.Id == conversationId);
            if (conversation is null || !CanAccess(conversation, senderId, senderIsAdmin))
                throw new ChatException("Nie znaleziono rozmowy.");

            var isSupport = conversation.Type == ConversationType.Support;
            var fromCustomer = isSupport && senderId == conversation.UserLowId;
            int? recipientId = null;
            string logAction;

            if (!isSupport)
            {
                recipientId = conversation.UserLowId == senderId
                    ? conversation.UserHighId
                    : conversation.UserLowId;

                var recipientActive = recipientId.HasValue
                    && await _context.Users.AsNoTracking().AnyAsync(u => u.Id == recipientId.Value && u.IsActive);

                if (!recipientActive)
                    throw new ChatException("Ten użytkownik jest niedostępny.");

                logAction = ChatLogActions.Sent;
            }
            else if (fromCustomer)
            {
                logAction = ChatLogActions.SupportSent;
            }
            else
            {
                recipientId = conversation.UserLowId;
                logAction = ChatLogActions.SupportReply;
            }

            var now = DateTime.UtcNow;

            var message = new ChatMessage
            {
                ConversationId = conversation.Id,
                SenderId = senderId,
                EncryptedContent = _encryption.Encrypt(content, EncryptionPurposes.Message),
                SentAt = now
            };
            _context.ChatMessages.Add(message);

            // Wiadomość i log w jednym SaveChanges (jedna transakcja).
            _context.ChatMessageLogs.Add(new ChatMessageLog
            {
                Message = message,
                ConversationId = conversation.Id,
                SenderId = senderId,
                RecipientId = recipientId,
                Action = logAction,
                EncryptedContent = _encryption.Encrypt(content, EncryptionPurposes.Log),
                IpAddress = string.IsNullOrWhiteSpace(ipAddress) ? null : Truncate(ipAddress, 64),
                CreatedAt = now
            });

            conversation.LastMessageAt = now;
            await _context.SaveChangesAsync();

            var senderName = isSupport && !fromCustomer
                ? ChatConstants.SupportDisplayName
                : (sender.UserName ?? string.Empty);

            var dto = new ChatMessageDTO
            {
                Id = message.Id,
                ConversationId = conversation.Id,
                SenderId = senderId,
                SenderName = senderName,
                Content = content,
                SentAt = DateTime.SpecifyKind(now, DateTimeKind.Utc),
                IsRead = false
            };

            await PublishAsync(conversation, senderId, dto);

            if (recipientId.HasValue)
            {
                if (isSupport)
                {
                    await NotifyRecipientAsync(
                        recipientId.Value,
                        ChatConstants.SupportDisplayName,
                        "Administracja odpowiedziała na Twoją wiadomość lub zgłoszenie.");
                }
                else
                {
                    await NotifyRecipientAsync(
                        recipientId.Value,
                        senderName,
                        $"{senderName} wysłał(a) do Ciebie wiadomość.");
                }
            }

            return dto;
        }

        public async Task<ChatMessageDTO> SendSupportMessageToUserAsync(
            int adminId, int customerId, string content, string? ipAddress)
        {
            if (adminId == customerId)
                throw new ChatException("Nie możesz odpowiedzieć samemu sobie.");

            var customerActive = await _context.Users
                .AsNoTracking()
                .AnyAsync(u => u.Id == customerId && u.IsActive);

            if (!customerActive)
                throw new ChatException("Ten użytkownik nie ma już aktywnego konta.");

            var conversation = await GetOrCreateSupportConversationAsync(customerId);
            return await SendMessageAsync(adminId, true, conversation.Id, content, ipAddress);
        }

        public async Task<bool> MarkAsReadAsync(int userId, bool isAdmin, int conversationId)
        {
            var conversation = await _context.Conversations.FirstOrDefaultAsync(c => c.Id == conversationId);
            if (conversation is null || !CanAccess(conversation, userId, isAdmin))
                return false;

            var adminView = IsAdminView(conversation, userId, isAdmin);
            var customerId = conversation.UserLowId;

            IQueryable<ChatMessage> query = _context.ChatMessages
                .Where(m => m.ConversationId == conversationId && !m.IsRead);

            query = adminView
                ? query.Where(m => m.SenderId == customerId)
                : query.Where(m => m.SenderId != userId);

            var unread = await query.ToListAsync();
            var now = DateTime.UtcNow;
            foreach (var message in unread)
            {
                message.IsRead = true;
                message.ReadAt = now;
            }

            if (!adminView)
            {
                string? actorName;
                if (conversation.Type == ConversationType.Support)
                {
                    actorName = ChatConstants.SupportDisplayName;
                }
                else
                {
                    var otherId = conversation.UserLowId == userId
                        ? conversation.UserHighId ?? 0
                        : conversation.UserLowId;

                    actorName = await _context.Users
                        .AsNoTracking()
                        .Where(u => u.Id == otherId)
                        .Select(u => u.UserName)
                        .FirstOrDefaultAsync();
                }

                if (!string.IsNullOrEmpty(actorName))
                {
                    var notifications = await _context.Notifications
                        .Where(n => n.UserId == userId
                            && n.Type == NotificationType.NewMessage
                            && !n.IsRead
                            && n.ActorName == actorName)
                        .ToListAsync();

                    foreach (var notification in notifications)
                        notification.IsRead = true;
                }
            }

            await _context.SaveChangesAsync();
            return true;
        }

        // ---------- Pomocnicze ----------

        private static bool CanAccess(Conversation conversation, int userId, bool isAdmin) =>
            conversation.Type == ConversationType.Direct
                ? (conversation.UserLowId == userId || conversation.UserHighId == userId)
                : (conversation.UserLowId == userId || isAdmin);

        private static bool IsAdminView(Conversation conversation, int userId, bool isAdmin) =>
            isAdmin && conversation.Type == ConversationType.Support && conversation.UserLowId != userId;

        private void EnforceRateLimit(int userId)
        {
            var counter = _cache.GetOrCreate($"chat-rate:{userId}", entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = RateLimitWindow;
                return new StrongBox<int>(0);
            })!;

            if (Interlocked.Increment(ref counter.Value) > RateLimitMessages)
                throw new ChatException("Wysyłasz wiadomości zbyt szybko. Poczekaj chwilę.");
        }

        private async Task PublishAsync(Conversation conversation, int senderId, ChatMessageDTO dto)
        {
            var groups = new HashSet<string> { ChatHub.UserGroup(senderId) };

            if (conversation.Type == ConversationType.Direct)
            {
                groups.Add(ChatHub.UserGroup(conversation.UserLowId));
                if (conversation.UserHighId.HasValue)
                    groups.Add(ChatHub.UserGroup(conversation.UserHighId.Value));
            }
            else
            {
                groups.Add(ChatHub.UserGroup(conversation.UserLowId));
                groups.Add(ChatHub.AdminsGroup);
            }

            try
            {
                await _hubContext.Clients.Groups(groups.ToList()).SendAsync("ReceiveMessage", dto);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Nie udało się wysłać wiadomości {MessageId} przez SignalR.", dto.Id);
            }
        }

        private async Task NotifyRecipientAsync(int recipientId, string actorName, string text)
        {
            try
            {
                var hasUnread = await _context.Notifications
                    .AsNoTracking()
                    .AnyAsync(n => n.UserId == recipientId
                        && n.Type == NotificationType.NewMessage
                        && !n.IsRead
                        && n.ActorName == actorName);

                if (hasUnread)
                    return;

                await _notificationService.CreateAsync(
                    recipientId,
                    NotificationType.NewMessage,
                    "Nowa wiadomość",
                    text,
                    null,
                    actorName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Nie udało się utworzyć powiadomienia o wiadomości dla użytkownika {UserId}.", recipientId);
            }
        }

        private string SafeDecrypt(string payload, string purpose)
        {
            try
            {
                return _encryption.Decrypt(payload, purpose);
            }
            catch (Exception ex) when (ex is CryptographicException or FormatException)
            {
                _logger.LogError(ex, "Nie udało się odszyfrować wiadomości (zmieniony klucz?).");
                return "[nie można odszyfrować wiadomości]";
            }
        }

        private static string Truncate(string value, int max) =>
            value.Length <= max ? value : value[..max];

        private static string ToPreview(string text)
        {
            var flat = text.Replace("\r", " ").Replace("\n", " ").Trim();
            return flat.Length <= PreviewLength ? flat : flat[..PreviewLength] + "…";
        }

        private async Task<List<ChatMessageDTO>> ToMessageDtosAsync(Conversation conversation, List<ChatMessage> messages)
        {
            var senderIds = messages.Select(m => m.SenderId).Distinct().ToList();

            var names = await _context.Users
                .AsNoTracking()
                .Where(u => senderIds.Contains(u.Id))
                .Select(u => new { u.Id, u.UserName })
                .ToDictionaryAsync(u => u.Id, u => u.UserName ?? string.Empty);

            return messages.Select(m => new ChatMessageDTO
            {
                Id = m.Id,
                ConversationId = m.ConversationId,
                SenderId = m.SenderId,
                SenderName = conversation.Type == ConversationType.Support && m.SenderId != conversation.UserLowId
                    ? ChatConstants.SupportDisplayName
                    : names.GetValueOrDefault(m.SenderId, "Usunięty użytkownik"),
                Content = SafeDecrypt(m.EncryptedContent, EncryptionPurposes.Message),
                SentAt = DateTime.SpecifyKind(m.SentAt, DateTimeKind.Utc),
                IsRead = m.IsRead
            }).ToList();
        }

        private async Task<List<ConversationDTO>> BuildConversationDtosAsync(
            List<Conversation> conversations, int viewerId, bool adminView)
        {
            if (conversations.Count == 0)
                return new List<ConversationDTO>();

            var convIds = conversations.Select(c => c.Id).ToList();

            var userIds = conversations
                .SelectMany(c => new[] { c.UserLowId, c.UserHighId ?? 0 })
                .Where(id => id > 0)
                .Distinct()
                .ToList();

            var users = await _context.Users
                .AsNoTracking()
                .Where(u => userIds.Contains(u.Id))
                .Select(u => new { u.Id, u.UserName, u.AvatarPath })
                .ToListAsync();
            var userMap = users.ToDictionary(u => u.Id);

            var productIds = conversations
                .Where(c => c.ProductId.HasValue)
                .Select(c => c.ProductId!.Value)
                .Distinct()
                .ToList();

            var productMap = productIds.Count == 0
                ? new Dictionary<int, string>()
                : await _context.Products
                    .AsNoTracking()
                    .Where(p => productIds.Contains(p.Id))
                    .Select(p => new { p.Id, p.Name })
                    .ToDictionaryAsync(p => p.Id, p => p.Name);

            var lastMessages = await _context.ChatMessages
                .AsNoTracking()
                .Where(m => convIds.Contains(m.ConversationId)
                    && m.Id == _context.ChatMessages
                        .Where(x => x.ConversationId == m.ConversationId)
                        .Max(x => x.Id))
                .ToListAsync();
            var lastByConversation = lastMessages.ToDictionary(m => m.ConversationId);

            var unreadRows = await _context.ChatMessages
                .AsNoTracking()
                .Where(m => convIds.Contains(m.ConversationId) && !m.IsRead)
                .GroupBy(m => new { m.ConversationId, m.SenderId })
                .Select(g => new { g.Key.ConversationId, g.Key.SenderId, Count = g.Count() })
                .ToListAsync();

            var result = new List<ConversationDTO>();

            foreach (var c in conversations)
            {
                var dto = new ConversationDTO
                {
                    Id = c.Id,
                    Type = c.Type,
                    ProductId = c.ProductId,
                    LastMessageAt = DateTime.SpecifyKind(c.LastMessageAt, DateTimeKind.Utc)
                };

                if (c.ProductId.HasValue && productMap.TryGetValue(c.ProductId.Value, out var productName))
                    dto.ProductName = productName;

                if (c.Type == ConversationType.Support)
                {
                    if (adminView)
                    {
                        dto.OtherUserId = c.UserLowId;
                        if (userMap.TryGetValue(c.UserLowId, out var customer))
                        {
                            dto.OtherUserName = customer.UserName ?? string.Empty;
                            dto.OtherUserAvatarUrl = AvatarUrl(customer.AvatarPath);
                        }
                        else
                        {
                            dto.OtherUserName = "Usunięty użytkownik";
                        }
                    }
                    else
                    {
                        dto.OtherUserName = ChatConstants.SupportDisplayName;
                    }
                }
                else
                {
                    var otherId = c.UserLowId == viewerId ? (c.UserHighId ?? 0) : c.UserLowId;
                    dto.OtherUserId = otherId;
                    if (userMap.TryGetValue(otherId, out var other))
                    {
                        dto.OtherUserName = other.UserName ?? string.Empty;
                        dto.OtherUserAvatarUrl = AvatarUrl(other.AvatarPath);
                    }
                    else
                    {
                        dto.OtherUserName = "Usunięty użytkownik";
                    }
                }

                if (lastByConversation.TryGetValue(c.Id, out var last))
                {
                    dto.LastMessage = ToPreview(SafeDecrypt(last.EncryptedContent, EncryptionPurposes.Message));
                }

                dto.UnreadCount = adminView
                    ? unreadRows.Where(r => r.ConversationId == c.Id && r.SenderId == c.UserLowId).Sum(r => r.Count)
                    : unreadRows.Where(r => r.ConversationId == c.Id && r.SenderId != viewerId).Sum(r => r.Count);

                result.Add(dto);
            }

            return result;
        }

        private static string? AvatarUrl(string? avatarPath) =>
            string.IsNullOrEmpty(avatarPath) ? null : $"/avatars/{avatarPath}";
    }
}