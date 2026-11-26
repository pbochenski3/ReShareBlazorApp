using Microsoft.EntityFrameworkCore;
using ReShareBlazorApp.Api.Date;
using ReShareBlazorApp.Api.Interfaces.Services;
using ReShareBlazorApp.Shared;
using ReShareBlazorApp.Shared.DTOs;
using ReShareBlazorApp.Shared.Enums;

namespace ReShareBlazorApp.Api.Services
{
    public class MessageService : IMessageService
    {
        private readonly ReshareDbContext _context;
        private readonly IMessageEncryptionService _encryptionService;
        private readonly INotificationService _notificationService;
        private readonly IActivityLogger _activityLogger;
        private readonly ILogger<MessageService> _logger;

        public MessageService(
            ReshareDbContext context,
            IMessageEncryptionService encryptionService,
            INotificationService notificationService,
            IActivityLogger activityLogger,
            ILogger<MessageService> logger)
        {
            _context = context;
            _encryptionService = encryptionService;
            _notificationService = notificationService;
            _activityLogger = activityLogger;
            _logger = logger;
        }

        public async Task<List<ConversationSummaryDTO>> GetConversationsAsync(int currentUserId)
        {
            try
            {
                // Pobieramy wszystkie wiadomości, w których uczestniczy obecny użytkownik
                var userMessages = await _context.ChatMessages
                    .AsNoTracking()
                    .Include(m => m.Sender)
                    .Include(m => m.Recipient)
                    .Include(m => m.Product)
                    .Where(m => m.SenderId == currentUserId || m.RecipientId == currentUserId)
                    .OrderByDescending(m => m.CreatedAt)
                    .ToListAsync();

                // Grupujemy po rozmówcy (OtherUserId)
                var grouped = userMessages
                    .GroupBy(m => m.SenderId == currentUserId ? m.RecipientId : m.SenderId);

                var conversations = new List<ConversationSummaryDTO>();

                foreach (var group in grouped)
                {
                    var partnerId = group.Key;
                    var latest = group.First();

                    var partner = latest.SenderId == currentUserId ? latest.Recipient : latest.Sender;
                    if (partner == null)
                    {
                        partner = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == partnerId);
                    }

                    var unreadCount = group.Count(m => m.RecipientId == currentUserId && !m.IsRead);
                    var decryptedLastMessage = _encryptionService.Decrypt(latest.EncryptedContent);

                    // Jeśli propozycja ceny, dodaj info
                    if (latest.OfferPrice.HasValue)
                    {
                        decryptedLastMessage = $"[Oferta: {latest.OfferPrice.Value:0.##} zł] {decryptedLastMessage}".Trim();
                    }

                    conversations.Add(new ConversationSummaryDTO
                    {
                        OtherUserId = partnerId,
                        OtherUserName = partner?.UserName ?? $"Użytkownik #{partnerId}",
                        OtherUserAvatar = partner?.AvatarPath,
                        ProductId = latest.ProductId,
                        ProductName = latest.Product?.Name,
                        LastMessage = decryptedLastMessage,
                        LastMessageDate = latest.CreatedAt,
                        UnreadCount = unreadCount
                    });
                }

                return conversations.OrderByDescending(c => c.LastMessageDate).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MessageService] Błąd podczas pobierania listy konwersacji.");
                return new List<ConversationSummaryDTO>();
            }
        }

        public async Task<List<ChatMessageDTO>> GetConversationMessagesAsync(int currentUserId, int otherUserId, int? productId = null)
        {
            try
            {
                var query = _context.ChatMessages
                    .Include(m => m.Sender)
                    .Include(m => m.Recipient)
                    .Include(m => m.Product)
                    .Where(m => (m.SenderId == currentUserId && m.RecipientId == otherUserId) ||
                                (m.SenderId == otherUserId && m.RecipientId == currentUserId));

                if (productId.HasValue)
                {
                    // Opcjonalne filtrowanie po produkcie lub wspólna historia
                    query = query.Where(m => m.ProductId == productId.Value || m.ProductId == null);
                }

                var messages = await query
                    .OrderBy(m => m.CreatedAt)
                    .ToListAsync();

                // Oznaczamy wiadomości od rozmówcy jako przeczytane
                var unread = messages.Where(m => m.RecipientId == currentUserId && !m.IsRead).ToList();
                if (unread.Any())
                {
                    foreach (var msg in unread)
                    {
                        msg.IsRead = true;
                    }
                    await _context.SaveChangesAsync();
                }

                return messages.Select(m => new ChatMessageDTO
                {
                    Id = m.Id,
                    SenderId = m.SenderId,
                    SenderName = m.Sender?.UserName ?? "",
                    SenderAvatar = m.Sender?.AvatarPath,
                    RecipientId = m.RecipientId,
                    RecipientName = m.Recipient?.UserName ?? "",
                    RecipientAvatar = m.Recipient?.AvatarPath,
                    ProductId = m.ProductId,
                    ProductName = m.Product?.Name,
                    Content = _encryptionService.Decrypt(m.EncryptedContent),
                    CreatedAt = m.CreatedAt,
                    IsRead = m.IsRead,
                    OfferPrice = m.OfferPrice,
                    IsFromCurrentUser = m.SenderId == currentUserId
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MessageService] Błąd podczas pobierania wiadomości konwersacji.");
                return new List<ChatMessageDTO>();
            }
        }

        public async Task<ChatMessageDTO?> SendMessageAsync(int currentUserId, SendMessageRequest request)
        {
            if (currentUserId == request.RecipientId)
            {
                _logger.LogWarning("[MessageService] Użytkownik {UserId} próbował wysłać wiadomość do samego siebie.", currentUserId);
                return null;
            }

            var sender = await _context.Users.FindAsync(currentUserId);
            var recipient = await _context.Users.FindAsync(request.RecipientId);

            if (sender == null || recipient == null || recipient.IsDeleted)
            {
                _logger.LogWarning("[MessageService] Nie znaleziono nadawcy lub odbiorcy wiadomości.");
                return null;
            }

            Product? product = null;
            if (request.ProductId.HasValue)
            {
                product = await _context.Products.FindAsync(request.ProductId.Value);
            }

            // Szyfrowanie treści wiadomości (AES-256)
            var encryptedContent = _encryptionService.Encrypt(request.Content.Trim());

            var message = new ChatMessage
            {
                SenderId = currentUserId,
                RecipientId = request.RecipientId,
                ProductId = request.ProductId,
                EncryptedContent = encryptedContent,
                OfferPrice = request.OfferPrice,
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };

            _context.ChatMessages.Add(message);
            await _context.SaveChangesAsync();

            // Zapis zaszyfrowanego logu wiadomości w dedykowanej tabeli MessageLogs
            var messageLog = new MessageLog
            {
                MessageId = message.Id,
                SenderId = currentUserId,
                SenderName = sender.UserName ?? "",
                RecipientId = request.RecipientId,
                RecipientName = recipient.UserName ?? "",
                ProductId = request.ProductId,
                ProductName = product?.Name,
                EncryptedContent = encryptedContent, // Zaszyfrowany zapis
                CreatedAt = DateTime.UtcNow
            };
            _context.MessageLogs.Add(messageLog);
            await _context.SaveChangesAsync();

            // Zapis w dzienniku zdarzeń (ActivityLogger) - zaszyfrowany wycinek lub token w opisie
            var encryptedSnippet = encryptedContent.Length > 80 ? encryptedContent[..80] + "..." : encryptedContent;
            await _activityLogger.LogAsync(
                currentUserId,
                sender.UserName,
                ActivityActions.MessageSent,
                $"Wysłano wiadomość do: {recipient.UserName} (ogłoszenie id: {request.ProductId}). Zaszyfrowana treść: {encryptedSnippet}",
                "ChatMessage",
                message.Id);

            // Wysłanie powiadomienia do odbiorcy
            try
            {
                var notifTitle = $"Nowa wiadomość od {sender.UserName}";
                var notifBody = product != null
                    ? $"Otrzymałeś nową wiadomość dotyczącą ogłoszenia \"{product.Name}\"."
                    : $"Otrzymałeś nową wiadomość od użytkownika {sender.UserName}.";

                await _notificationService.CreateAsync(
                    request.RecipientId,
                    NotificationType.NewMessage,
                    notifTitle,
                    notifBody,
                    request.ProductId,
                    sender.UserName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MessageService] Błąd wysyłania powiadomienia o nowej wiadomości.");
            }

            return new ChatMessageDTO
            {
                Id = message.Id,
                SenderId = currentUserId,
                SenderName = sender.UserName ?? "",
                SenderAvatar = sender.AvatarPath,
                RecipientId = request.RecipientId,
                RecipientName = recipient.UserName ?? "",
                RecipientAvatar = recipient.AvatarPath,
                ProductId = message.ProductId,
                ProductName = product?.Name,
                Content = request.Content.Trim(),
                CreatedAt = message.CreatedAt,
                IsRead = message.IsRead,
                OfferPrice = message.OfferPrice,
                IsFromCurrentUser = true
            };
        }

        public async Task<bool> MarkConversationAsReadAsync(int currentUserId, int otherUserId)
        {
            try
            {
                var unread = await _context.ChatMessages
                    .Where(m => m.SenderId == otherUserId && m.RecipientId == currentUserId && !m.IsRead)
                    .ToListAsync();

                if (!unread.Any())
                    return true;

                foreach (var msg in unread)
                {
                    msg.IsRead = true;
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MessageService] Błąd podczas oznaczania konwersacji jako przeczytanej.");
                return false;
            }
        }
    }
}
