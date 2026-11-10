using ReShareBlazorApp.Shared.DTOs;

namespace ReShareBlazorApp.Web.ApiServices
{
    public class NotificationService
    {
        private readonly Queue<NotificationDTO> _notifications 
            = new Queue<NotificationDTO>();
        private readonly object _lock = new();
        private Timer? _timer;
        public NotificationDTO? CurrentNotification { get; private set; }
        public event Action? OnNotificationChanged;
        
        public void AddNotification(string title,string message)
        {
            lock (_lock)
            {
                _notifications.Enqueue(new NotificationDTO
                {
                    Title = title,
                    Message = message,
                    CreatedAt = DateTime.Now
                });
                if(CurrentNotification == null)
                {
                    ShowNextNotification();
                }
            }    
        }
        private void ShowNextNotification()
        {
            lock (_lock)
            {
                if(_notifications.Count == 0)
                {
                    CurrentNotification = null;
                    _timer?.Dispose();
                    _timer = null;
                }
                else
                {
                    CurrentNotification = _notifications.Dequeue();
                    _timer?.Dispose();
                    _timer = new Timer(_ =>
                    {
                        lock (_lock)
                        {
                            CurrentNotification = null;
                            ShowNextNotification();
                        }
                        OnNotificationChanged?.Invoke();
                    }, null, 3000, Timeout.Infinite);
                }
            }
            OnNotificationChanged?.Invoke();
        }
        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
