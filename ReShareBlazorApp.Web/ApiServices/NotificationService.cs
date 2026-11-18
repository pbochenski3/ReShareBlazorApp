using ReShareBlazorApp.Shared.DTOs;

namespace ReShareBlazorApp.Web.ApiServices
{
    public class NotificationService
    {
        public Queue<NotificationDTO> Notifications { get; set; }
        = new Queue<NotificationDTO>();

        private readonly object _lock = new object();

        private Timer? _timer;

        public NotificationDTO? CurrentNotification { get; private set; }
        public event Action? OnNotificationChanged;

        public void AddNotification(string title, string meesage)
        {
          lock (_lock)
            {
                Notifications.Enqueue(new NotificationDTO
                {
                    Title = title,
                    Message = meesage
                });
                if (CurrentNotification == null)
                {
                    ShowNextNotification();
                }
            }
        }
        private void ShowNextNotification()
        {
            lock (_lock)
            {
                if (Notifications.Count > 0)
                {
                    CurrentNotification = Notifications.Dequeue();
                    OnNotificationChanged?.Invoke();
                    _timer = new Timer(_ =>
                    {
                        CurrentNotification = null;
                        OnNotificationChanged?.Invoke();
                        ShowNextNotification();
                    }, null, 3000, Timeout.Infinite);
                }
                else
                {
                    CurrentNotification = null;
                    OnNotificationChanged?.Invoke();
                }
            }
        }
    }

}
