using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Text.RegularExpressions;

namespace MarketplaceArtesanato.Core.Hubs
{
    [Authorize(Roles = "Admin")]
    public class NotificationHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "Admins");
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "Admins");
            await base.OnDisconnectedAsync(exception);
        }

    }
}
