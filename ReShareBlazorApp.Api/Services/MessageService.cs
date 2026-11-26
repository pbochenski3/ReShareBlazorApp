using Microsoft.EntityFrameworkCore;
using ReShareBlazorApp.Api.Date;
using ReShareBlazorApp.Api.Interfaces.Services;
using ReShareBlazorApp.Shared.DTOs;
using ReShareBlazorApp.Shared.Enums;

namespace ReShareBlazorApp.Api.Services
{
    public class MessageService : IMessageService
    {
        private const string SupportDisplayName = "Support";
        private const int MaxContentLength = 2000;

        private readonly ReshareDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly ILogger<MessageService> _logger;

        public MessageService(
            ReshareDbContext context,
            INotificationService notificationService,
            ILogger<MessageService> logger)
        {
            _context = context;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task<List<ConversationDTO>> GetConversationsAsync(int userId)
        {
            var conversations = await _context.Conversations
                .AsNoTracking()
                .Where(c => c.User1Id == userId || c.User2Id == userId)
                .Include(c => c.Product)
                .Include(c => c.User1)
                .Include(c => c.User2)
                .Include(c => c.Messages.OrderByDescending(m => m.SentAt).Take(1))
                .OrderByDescending(c => c.LastMessageAt)
                .ToListAsync();

            if (conversations.Count == 0)
                return new List<ConversationDTO>();

            var conversationIds = conversations.Select(c => c.Id).ToList();

            var unreadCounts = await _context.Messages
                .AsNoTracking()
                .Where(m => conversationIds.Contains(m.ConversationId)
                    && m.SenderId != userId
                    && !m.IsReadByRecipient)
                .GroupBy(m => m.ConversationId)
                .Select(g => new { ConversationId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.ConversationId, x => x.Count);

            return conversations
                .Select(c => MapConversation(
                    c,
                    userId,
                    unreadCounts.GetValueOrDefault(c.Id),
                    c.Messages.OrderByDescending(m => m.SentAt).FirstOrDefault()?.Content))
                .ToList();
        }

        public async Task<(ConversationDTO Conversation, List<MessageDTO> Messages)?> GetMessagesAsync(
            int conversationId, int userId)
        {
            var conversation = await _context.Conversations
                .Include(c => c.Product)
                .Include(c => c.User1)
                .Include(c => c.User2)
                .FirstOrDefaultAsync(c => c.Id == conversationId);

            if (conversation == null || !IsParticipant(conversation, userId))
                return null;

            var messages = await _context.Messages
                .Include(m => m.Sender)
                .Where(m => m.ConversationId == conversationId)
                .OrderBy(m => m.SentAt)
                .ToListAsync();

            // Wiadomości drugiej strony oznaczamy jako przeczytane, skoro użytkownik je właśnie otwiera.
            var unreadFromOther = messages.Where(m => m.SenderId != userId && !m.IsReadByRecipient).ToList();
            if (unreadFromOther.Count > 0)
            {
                foreach (var message in unreadFromOther)
                    message.IsReadByRecipient = true;

                await _context.SaveChangesAsync();
            }

            var adminId = conversation.IsSupport ? conversation.User1Id : (int?)null;

            var messageDtos = messages
                .Select(m => new MessageDTO
                {
                    Id = m.Id,
                    ConversationId = m.ConversationId,
                    SenderId = m.SenderId,
                    SenderName = conversation.IsSupport && m.SenderId == adminId
                        ? SupportDisplayName
                        : m.Sender?.UserName ?? "",
                    Content = m.Content,
                    SentAt = m.SentAt,
                    IsMine = m.SenderId == userId
                })
                .ToList();

            var conversationDto = MapConversation(
                conversation, userId, unreadCount: 0, lastMessageOverride: messages.LastOrDefault()?.Content);

            return (conversationDto, messageDtos);
        }

        public async Task<ConversationDTO?> StartConversationAsync(int senderId, StartConversationRequest request)
        {
            var content = (request.Content ?? "").Trim();
            if (content.Length == 0 || content.Length > MaxContentLength)
                return null;

            if (request.RecipientId == senderId)
                return null;

            var product = await _context.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == request.ProductId);

            if (product == null)
                return null;

            var recipientExists = await _context.Users.AnyAsync(u => u.Id == request.RecipientId);
            if (!recipientExists)
                return null;

            var conversation = await _context.Conversations
                .FirstOrDefaultAsync(c =>
                    !c.IsSupport &&
                    c.ProductId == request.ProductId &&
                    ((c.User1Id == senderId && c.User2Id == request.RecipientId) ||
                     (c.User1Id == request.RecipientId && c.User2Id == senderId)));

            if (conversation == null)
            {
                conversation = new Conversation
                {
                    ProductId = request.ProductId,
                    User1Id = senderId,
                    User2Id = request.RecipientId,
                    IsSupport = false,
                    CreatedAt = DateTime.UtcNow,
                    LastMessageAt = DateTime.UtcNow
                };
                _context.Conversations.Add(conversation);
                await _context.SaveChangesAsync();
            }

            var sentMessage = await AddMessageInternalAsync(conversation, senderId, content);

            await _context.Entry(conversation).Reference(c => c.Product).LoadAsync();
            await _context.Entry(conversation).Reference(c => c.User1).LoadAsync();
            await _context.Entry(conversation).Reference(c => c.User2).LoadAsync();

            return MapConversation(conversation, senderId, unreadCount: 0, sentMessage.Content);
        }

        public async Task<MessageDTO?> SendMessageAsync(int senderId, int conversationId, string content)
        {
            var trimmed = (content ?? "").Trim();
            if (trimmed.Length == 0 || trimmed.Length > MaxContentLength)
                return null;

            var conversation = await _context.Conversations
                .Include(c => c.User1)
                .Include(c => c.User2)
                .FirstOrDefaultAsync(c => c.Id == conversationId);

            if (conversation == null || !IsParticipant(conversation, senderId))
                return null;

            var message = await AddMessageInternalAsync(conversation, senderId, trimmed);

            return new MessageDTO
            {
                Id = message.Id,
                ConversationId = conversation.Id,
                SenderId = senderId,
                SenderName = conversation.IsSupport && senderId == conversation.User1Id
                    ? SupportDisplayName
                    : message.Sender?.UserName ?? "",
                Content = message.Content,
                SentAt = message.SentAt,
                IsMine = true
            };
        }

        public async Task<ConversationDTO?> StartSupportConversationAsync(int adminId, int reportId, string content)
        {
            var trimmed = (content ?? "").Trim();
            if (trimmed.Length == 0 || trimmed.Length > MaxContentLength)
                return null;

            var report = await _context.Reports
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == reportId);

            if (report == null)
                return null;

            var conversation = await _context.Conversations
                .FirstOrDefaultAsync(c => c.IsSupport && c.ReportId == reportId);

            if (conversation == null)
            {
                conversation = new Conversation
                {
                    ProductId = null,
                    User1Id = adminId,
                    User2Id = report.UserId,
                    IsSupport = true,
                    ReportId = reportId,
                    CreatedAt = DateTime.UtcNow,
                    LastMessageAt = DateTime.UtcNow
                };
                _context.Conversations.Add(conversation);
                await _context.SaveChangesAsync();
            }

            var sentMessage = await AddMessageInternalAsync(conversation, adminId, trimmed);

            await _context.Entry(conversation).Reference(c => c.User1).LoadAsync();
            await _context.Entry(conversation).Reference(c => c.User2).LoadAsync();

            return MapConversation(conversation, adminId, unreadCount: 0, sentMessage.Content);
        }

        private async Task<Message> AddMessageInternalAsync(Conversation conversation, int senderId, string content)
        {
            var message = new Message
            {
                ConversationId = conversation.Id,
                SenderId = senderId,
                Content = Limit(content, MaxContentLength),
                SentAt = DateTime.UtcNow,
                IsReadByRecipient = false
            };

            _context.Messages.Add(message);
            conversation.LastMessageAt = message.SentAt;
            await _context.SaveChangesAsync();

            try
            {
                await NotifyRecipientAsync(conversation, senderId, message.Content);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception,
                    "Nie udało się utworzyć powiadomienia o nowej wiadomości w rozmowie {ConversationId}",
                    conversation.Id);
            }

            await _context.Entry(message).Reference(m => m.Sender).LoadAsync();
            return message;
        }

        private async Task NotifyRecipientAsync(Conversation conversation, int senderId, string content)
        {
            var recipientId = conversation.User1Id == senderId ? conversation.User2Id : conversation.User1Id;
            var preview = content.Length > 120 ? content[..120] + "…" : content;

            if (conversation.IsSupport)
            {
                // Tylko support (administrator, User1) pisze pierwszy; odpowiedź zgłaszającego trafia do admina pod jego własną nazwą.
                var actorName = senderId == conversation.User1Id ? SupportDisplayName : null;
                await _notificationService.CreateAsync(
                    recipientId,
                    NotificationType.NewMessage,
                    senderId == conversation.User1Id ? "Nowa wiadomość od wsparcia" : "Nowa odpowiedź na zgłoszenie",
                    preview,
                    productId: null,
                    actorName: actorName);
                return;
            }

            var sender = await _context.Users.AsNoTracking()
                .Where(u => u.Id == senderId)
                .Select(u => u.UserName)
                .FirstOrDefaultAsync();

            await _notificationService.CreateAsync(
                recipientId,
                NotificationType.NewMessage,
                "Nowa wiadomość",
                preview,
                productId: conversation.ProductId,
                actorName: sender);
        }

        private static bool IsParticipant(Conversation conversation, int userId) =>
            conversation.User1Id == userId || conversation.User2Id == userId;

        private static ConversationDTO MapConversation(
            Conversation conversation, int viewerId, int unreadCount, string? lastMessageOverride = null)
        {
            var viewerIsUser1 = conversation.User1Id == viewerId;
            var otherUser = viewerIsUser1 ? conversation.User2 : conversation.User1;
            var otherUserId = viewerIsUser1 ? conversation.User2Id : conversation.User1Id;

            string otherUserName;
            string? productName;

            if (conversation.IsSupport)
            {
                // Zgłaszający widzi "Support"; administrator widzi, z kim rozmawia i w sprawie jakiego zgłoszenia.
                otherUserName = viewerIsUser1
                    ? $"{otherUser?.UserName} (wsparcie – zgł. #{conversation.ReportId})"
                    : SupportDisplayName;
                productName = $"Zgłoszenie #{conversation.ReportId}";
            }
            else
            {
                otherUserName = otherUser?.UserName ?? "Użytkownik";
                productName = conversation.Product?.Name;
            }

            return new ConversationDTO
            {
                ConversationId = conversation.Id,
                ProductId = conversation.ProductId,
                ProductName = productName,
                OtherUserId = otherUserId,
                OtherUserName = otherUserName,
                IsSupport = conversation.IsSupport,
                LastMessage = lastMessageOverride,
                LastMessageAt = conversation.LastMessageAt,
                UnreadCount = unreadCount
            };
        }

        private static string Limit(string value, int maxLength) =>
            value.Length <= maxLength ? value : value[..maxLength];
    }
}
