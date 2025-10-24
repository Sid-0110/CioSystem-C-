using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;

namespace CioSystem.Web.Hubs
{
    public class DashboardHub : Hub
    {
        public async Task JoinGroup(string groupName)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            await Clients.Group(groupName).SendAsync("UserJoined", Context.ConnectionId);
        }

        public async Task LeaveGroup(string groupName)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
            await Clients.Group(groupName).SendAsync("UserLeft", Context.ConnectionId);
        }

        public async Task JoinRoleGroup(string role)
        {
            var groupName = $"role_{role}";
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        }

        public async Task LeaveRoleGroup(string role)
        {
            var groupName = $"role_{role}";
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        }

        public override async Task OnConnectedAsync()
        {
            // 預設加入所有用戶群組
            await Groups.AddToGroupAsync(Context.ConnectionId, "all_users");
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await base.OnDisconnectedAsync(exception);
        }
    }
}

