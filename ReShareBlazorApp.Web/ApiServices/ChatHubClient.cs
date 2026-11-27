using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using ReShareBlazorApp.Shared.DTOs;

namespace ReShareBlazorApp.Web.ApiServices
{
    // Jedno połączenie SignalR na obwód Blazora (scoped).
    public class ChatHubClient : IAsyncDisposable
    {
        private readonly IConfiguration _configuration;
        private readonly AuthenticationStateProvider _authStateProvider;
        private readonly SemaphoreSlim _lock = new(1, 1);
        private HubConnection? _connection;

        public event Action<ChatMessageDTO>? MessageReceived;
        public event Action<NotificationDTO>? NotificationReceived;
        public event Action? ConnectionStateChanged;

        public ChatHubClient(IConfiguration configuration, AuthenticationStateProvider authStateProvider)
        {
            _configuration = configuration;
            _authStateProvider = authStateProvider;
        }

        public bool IsConnected => _connection?.State == HubConnectionState.Connected;

        public async Task EnsureConnectedAsync()
        {
            await _lock.WaitAsync();
            try
            {
                if (_connection is null)
                {
                    var authState = await _authStateProvider.GetAuthenticationStateAsync();
                    var token = authState.User.FindFirst("access_token")?.Value;
                    if (string.IsNullOrEmpty(token))
                        throw new InvalidOperationException("Brak tokenu sesji.");

                    var baseAddress = _configuration["ApiSettings:BaseAddress"]
                        ?? throw new InvalidOperationException("Brak ApiSettings:BaseAddress.");

                    var hubUrl = new Uri(
                        new Uri(baseAddress.TrimEnd('/') + "/"),
                        ChatConstants.HubPath.TrimStart('/'));

                    _connection = new HubConnectionBuilder()
                        .WithUrl(hubUrl, options =>
                        {
                            options.AccessTokenProvider = () => Task.FromResult<string?>(token);
                        })
                        .WithAutomaticReconnect()
                        .Build();

                    _connection.On<ChatMessageDTO>("ReceiveMessage", message =>
                    {
                        MessageReceived?.Invoke(message);
                    });

                    _connection.On<NotificationDTO>("ReceiveNotification", notification =>
                    {
                        NotificationReceived?.Invoke(notification);
                    });

                    _connection.Reconnecting += _ =>
                    {
                        ConnectionStateChanged?.Invoke();
                        return Task.CompletedTask;
                    };
                    _connection.Reconnected += _ =>
                    {
                        ConnectionStateChanged?.Invoke();
                        return Task.CompletedTask;
                    };
                    _connection.Closed += _ =>
                    {
                        ConnectionStateChanged?.Invoke();
                        return Task.CompletedTask;
                    };
                }

                if (_connection.State == HubConnectionState.Disconnected)
                    await _connection.StartAsync();
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task<(bool Success, string? Error)> SendMessageAsync(int conversationId, string content)
        {
            try
            {
                await EnsureConnectedAsync();
                await _connection!.InvokeAsync("SendMessage", conversationId, content);
                return (true, null);
            }
            catch (HubException ex)
            {
                const string marker = "HubException:";
                var index = ex.Message.IndexOf(marker, StringComparison.Ordinal);
                var text = index >= 0 ? ex.Message[(index + marker.Length)..].Trim() : string.Empty;
                return (false, string.IsNullOrWhiteSpace(text) ? "Nie udało się wysłać wiadomości." : text);
            }
            catch (Exception)
            {
                return (false, "Brak połączenia z czatem. Spróbuj ponownie za chwilę.");
            }
        }

        public async ValueTask DisposeAsync()
        {
            MessageReceived = null;
            NotificationReceived = null;
            ConnectionStateChanged = null;

            if (_connection is not null)
            {
                try { await _connection.DisposeAsync(); }
                catch (Exception) { }
            }

            _lock.Dispose();
        }
    }
}