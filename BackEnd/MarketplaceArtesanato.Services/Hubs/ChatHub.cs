using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using MarketplaceArtesanato.Data.Data;
using MarketplaceArtesanato.Core.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using System.Security.Claims;

namespace MarketplaceArtesanato.API.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly ArtesianDbContext _context;

        public ChatHub(ArtesianDbContext context)
        {
            _context = context;
        }
        public override async Task OnConnectedAsync()
        {
            var userName = Context.User?.Identity?.Name ?? "Anônimo";
            Console.WriteLine($"Usuário conectado no chat: {userName} ({Context.ConnectionId})");
            await base.OnConnectedAsync();
        }
        public async Task SendMessage(string user, string message)
        {
            await Clients.All.SendAsync("ReceiveMessage", user, message);
        }

        public async Task SendPrivateMessage(string userId, string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return;
            if (!Guid.TryParse(userId, out var recipientId)) return;

            // Obtém o ID do usuário autenticado a partir das claims
            var senderIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var senderId = Guid.TryParse(senderIdClaim, out var parsedSenderId) ? parsedSenderId : Guid.Empty;
            if (senderId == Guid.Empty) return;

            var senderIsSeller = Context.User?.IsInRole("Seller") == true;

            Guid sellerId;
            Guid customerId;
            Guid sellerUserId;

            if (senderIsSeller)
            {
                var seller = await _context.Sellers.AsNoTracking().FirstOrDefaultAsync(s => s.UserId == senderId);
                if (seller == null) return;

                sellerId = seller.Id;
                sellerUserId = seller.UserId;
                customerId = recipientId;
            }
            else
            {
                var seller = await _context.Sellers.AsNoTracking().FirstOrDefaultAsync(s => s.UserId == recipientId);
                if (seller == null) return;

                sellerId = seller.Id;
                sellerUserId = seller.UserId;
                customerId = senderId;
            }

            var chatMessage = new ChatMessage
            {
                SellerId = sellerId,
                CustomerId = customerId,
                SenderUserId = senderId,
                Message = message
            };

            _context.ChatMessages.Add(chatMessage);
            await _context.SaveChangesAsync();

            await Clients.Users(userId, senderId.ToString())
                .SendAsync("ReceivePrivateMessage", senderId.ToString(), message);

            if (!senderIsSeller)
            {
                var title = message.StartsWith("Solicitacao de contato:", StringComparison.OrdinalIgnoreCase)
                    ? "Nova solicitacao de contato"
                    : "Nova mensagem no chat";
                await Clients.User(sellerUserId.ToString())
                    .SendAsync("ReceiveNotification", title, message);
            }
        }

        public async Task SendNotification(string userId, string title, string message)
        {
            await Clients.User(userId).SendAsync("ReceiveNotification", title, message);
        }
    }
}
