using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.SignalR.Client;
using ReShareBlazorApp.Shared.DTOs;

namespace ReShareBlazorApp.Web.ApiServices;

public class ChatHubClientService : IAsyncDisposable
{
    private readonly IConfiguration _configuration;
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly NotificationApiService _notificationApi;
    private readonly SemaphoreSlim _connectLock = new(1, 1);

    private HubConnection? _hubConnection;
    private bool _disposed;

    public event Action<ChatMessageDTO>? OnMessageReceived;
    public event Action<int, int>? OnConversationRead;
    public event Action<int, int, string>? OnUserTyping;
    public event Action? OnUnreadCountChanged;
    public event Action? OnConnectionStateChanged;

    public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;

    public ChatHubClientService(
        IConfiguration configuration,
        AuthenticationStateProvider authStateProvider,
        NotificationApiService notificationApi)
    {
        _configuration = configuration;
        _authStateProvider = authStateProvider;
        _notificationApi = notificationApi;
    }

    public async Task EnsureConnectedAsync()
    {
        if (_disposed) return;

        var authState = await _authStateProvider.GetAuthenticationStateAsync();
        var token = authState.User.FindFirst("access_token")?.Value;
        if (string.IsNullOrEmpty(token))
            return;

        await _connectLock.WaitAsync();
        try
        {
            if (_disposed) return;

            if (_hubConnection is null)
            {
                var baseAddress = _configuration["ApiSettings:BaseAddress"]?.TrimEnd('/')
                    ?? throw new InvalidOperationException("Brak ApiSettings:BaseAddress.");

                _hubConnection = new HubConnectionBuilder()
                    .WithUrl($"{baseAddress}/hubs/chat", options =>
                    {
                        options.AccessTokenProvider = async () =>
                        {
                            var state = await _authStateProvider.GetAuthenticationStateAsync();
                            return state.User.FindFirst("access_token")?.Value;
                        };
                    })
                    .WithAutomaticReconnect()
                    .Build();

                _hubConnection.On<ChatMessageDTO>("ReceiveMessage", message =>
                {
                    OnMessageReceived?.Invoke(message);
                    OnUnreadCountChanged?.Invoke();
                    _notificationApi.NotifyNotificationsChanged();
                });

                _hubConnection.On<int, int>("ConversationRead", (conversationId, readerId) =>
                {
                    OnConversationRead?.Invoke(conversationId, readerId);
                });

                _hubConnection.On<int, int, string>("UserTyping", (conversationId, senderId, senderName) =>
                {
                    OnUserTyping?.Invoke(conversationId, senderId, senderName);
                });

                _hubConnection.On("UnreadCountChanged", () =>
                {
                    OnUnreadCountChanged?.Invoke();
                });

                _hubConnection.Reconnecting += _ =>
                {
                    OnConnectionStateChanged?.Invoke();
                    return Task.CompletedTask;
                };

                _hubConnection.Reconnected += _ =>
                {
                    OnConnectionStateChanged?.Invoke();
                    return Task.CompletedTask;
                };

                _hubConnection.Closed += _ =>
                {
                    OnConnectionStateChanged?.Invoke();
                    return Task.CompletedTask;
                };
            }

            if (_hubConnection.State == HubConnectionState.Disconnected)
            {
                await _hubConnection.StartAsync();
                OnConnectionStateChanged?.Invoke();
            }
        }
        catch
        {
            // Pozwalamy komponentom obsłużyć chwilowy brak połączenia przez fallback HTTP lub ponowne połączenie.
        }
        finally
        {
            _connectLock.Release();
        }
    }

    public async Task<ChatMessageDTO?> SendMessageAsync(SendChatMessageRequest request)
    {
        await EnsureConnectedAsync();
        if (_hubConnection is null || _hubConnection.State != HubConnectionState.Connected)
            return null;

        return await _hubConnection.InvokeAsync<ChatMessageDTO?>("SendMessage", request);
    }

    public async Task MarkConversationReadAsync(int conversationId)
    {
        if (_hubConnection is null || _hubConnection.State != HubConnectionState.Connected)
            return;

        try
        {
            await _hubConnection.InvokeAsync("MarkConversationRead", conversationId);
        }
        catch
        {
            // Ignorujemy przejściowe błędy sieciowe dla potwierdzenia odczytu.
        }
    }

    public async Task NotifyTypingAsync(int conversationId, int recipientId)
    {
        if (_hubConnection is null || _hubConnection.State != HubConnectionState.Connected)
            return;

        try
        {
            await _hubConnection.InvokeAsync("NotifyTyping", conversationId, recipientId);
        }
        catch
        {
            // Wskaźnik pisania jest opcjonalny.
        }
    }

    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        if (_hubConnection is not null)
        {
            await _hubConnection.DisposeAsync();
            _hubConnection = null;
        }
        _connectLock.Dispose();
    }
}
