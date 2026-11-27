using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using ReShareBlazorApp.Api.Interfaces.Services;
using System.Security.Claims;

namespace ReShareBlazorApp.Api.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        public const string AdminsGroup = "admins";
        public static string UserGroup(int userId) => $"user-{userId}";

        private readonly IChatService _chatService;

        public ChatHub(IChatService chatService)
        {
            _chatService = chatService;
        }

        private int CurrentUserId =>
            int.TryParse(Context.User?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

        private bool IsAdmin => Context.User?.IsInRole("Admin") == true;

        public override async Task OnConnectedAsync()
        {
            var userId = CurrentUserId;
            if (userId == 0)
            {
                Context.Abort();
                return;
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(userId));
            if (IsAdmin)
                await Groups.AddToGroupAsync(Context.ConnectionId, AdminsGroup);

            await base.OnConnectedAsync();
        }

        public async Task SendMessage(int conversationId, string content)
        {
            try
            {
                var ip = Context.GetHttpContext()?.Connection.RemoteIpAddress?.ToString();
                await _chatService.SendMessageAsync(CurrentUserId, IsAdmin, conversationId, content, ip);
            }
            catch (ChatException ex)
            {
                throw new HubException(ex.Message);
            }
        }
    }
}