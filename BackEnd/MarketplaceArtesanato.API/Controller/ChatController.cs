using MarketplaceArtesanato.API.Extensions;
using MarketplaceArtesanato.API.Hubs;
using MarketplaceArtesanato.Core.Entities;
using MarketplaceArtesanato.Core.Entities.Models.Responses;
using MarketplaceArtesanato.Data.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using MarketplaceArtesanato.Core.Entities.Models.Requests;

namespace MarketplaceArtesanato.API.Controllers
{
    [Route("api/chat")]
    [ApiController]
    [Authorize]
    public class ChatController : ControllerBase
    {
        private readonly ArtesianDbContext _context;
        private readonly IHubContext<ChatHub> _chatHub;

        public ChatController(ArtesianDbContext context, IHubContext<ChatHub> chatHub)
        {
            _context = context;
            _chatHub = chatHub;
        }

        [HttpGet("threads")]
        [Authorize(Roles = "Seller")]
        public async Task<ActionResult<List<ChatThreadResponseDto>>> GetThreads()
        {
            var userId = User.GetUserId();

            var sellerId = await _context.Sellers
                .Where(s => s.UserId == userId)
                .Select(s => s.Id)
                .FirstOrDefaultAsync();

            if (sellerId == Guid.Empty)
                return NotFound("Vendedor nao encontrado.");

            var messages = await _context.ChatMessages
                .AsNoTracking()
                .Where(m => m.SellerId == sellerId)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();

            if (messages.Count == 0)
                return Ok(new List<ChatThreadResponseDto>());

            var latestByCustomer = messages
                .GroupBy(m => m.CustomerId)
                .Select(g => g.First())
                .ToList();

            var customerIds = latestByCustomer.Select(m => m.CustomerId).Distinct().ToList();
            var customers = await _context.Users
                .AsNoTracking()
                .Where(u => customerIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id);

            var threads = latestByCustomer
                .Select(m =>
                {
                    customers.TryGetValue(m.CustomerId, out var customer);
                    return new ChatThreadResponseDto
                    {
                        CustomerId = m.CustomerId,
                        CustomerName = customer?.Name ?? "Cliente",
                        CustomerImageUrl = customer?.ProfileImageUrl,
                        LastMessage = m.Message,
                        LastMessageAt = m.CreatedAt
                    };
                })
                .OrderByDescending(t => t.LastMessageAt)
                .ToList();

            return Ok(threads);
        }

        [HttpGet("threads/contact-requests")]
        [Authorize(Roles = "Seller")]
        public async Task<ActionResult<List<ChatThreadResponseDto>>> GetContactRequestThreads()
        {
            var userId = User.GetUserId();

            var sellerId = await _context.Sellers
                .Where(s => s.UserId == userId)
                .Select(s => s.Id)
                .FirstOrDefaultAsync();

            if (sellerId == Guid.Empty)
                return NotFound("Vendedor nao encontrado.");

            var messages = await _context.ChatMessages
                .AsNoTracking()
                .Where(m => m.SellerId == sellerId)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();

            if (messages.Count == 0)
                return Ok(new List<ChatThreadResponseDto>());

            var byCustomer = messages
                .GroupBy(m => m.CustomerId)
                .Select(g => new
                {
                    CustomerId = g.Key,
                    Latest = g.First(),
                    HasContactRequest = g.Any(m =>
                        m.Message.StartsWith("Solicitacao de contato:", StringComparison.OrdinalIgnoreCase))
                })
                .Where(g => g.HasContactRequest)
                .ToList();

            if (byCustomer.Count == 0)
                return Ok(new List<ChatThreadResponseDto>());

            var customerIds = byCustomer.Select(m => m.CustomerId).Distinct().ToList();
            var customers = await _context.Users
                .AsNoTracking()
                .Where(u => customerIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id);

            var threads = byCustomer
                .Select(m =>
                {
                    customers.TryGetValue(m.CustomerId, out var customer);
                    return new ChatThreadResponseDto
                    {
                        CustomerId = m.CustomerId,
                        CustomerName = customer?.Name ?? "Cliente",
                        CustomerImageUrl = customer?.ProfileImageUrl,
                        LastMessage = m.Latest.Message,
                        LastMessageAt = m.Latest.CreatedAt
                    };
                })
                .OrderByDescending(t => t.LastMessageAt)
                .ToList();

            return Ok(threads);
        }

        [HttpGet("threads/customer")]
        public async Task<ActionResult<List<ChatCustomerThreadResponseDto>>> GetCustomerThreads()
        {
            var userId = User.GetUserId();
            if (User.IsInRole("Seller"))
                return Forbid();

            var messages = await _context.ChatMessages
                .AsNoTracking()
                .Where(m => m.CustomerId == userId)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();

            if (messages.Count == 0)
                return Ok(new List<ChatCustomerThreadResponseDto>());

            var latestBySeller = messages
                .GroupBy(m => m.SellerId)
                .Select(g => g.First())
                .ToList();

            var sellerIds = latestBySeller.Select(m => m.SellerId).Distinct().ToList();
            var sellers = await _context.Sellers
                .AsNoTracking()
                .Where(s => sellerIds.Contains(s.Id))
                .Select(s => new
                {
                    s.Id,
                    s.UserId,
                    s.StoreName,
                    s.ProfileImageUrl
                })
                .ToDictionaryAsync(s => s.Id);

            var threads = latestBySeller
                .Select(m =>
                {
                    sellers.TryGetValue(m.SellerId, out var seller);
                    return new ChatCustomerThreadResponseDto
                    {
                        SellerId = m.SellerId,
                        SellerUserId = seller?.UserId ?? Guid.Empty,
                        SellerName = seller?.StoreName ?? "Loja",
                        SellerImageUrl = seller?.ProfileImageUrl,
                        LastMessage = m.Message,
                        LastMessageAt = m.CreatedAt
                    };
                })
                .OrderByDescending(t => t.LastMessageAt)
                .ToList();

            return Ok(threads);
        }

        [HttpGet("messages")]
        public async Task<ActionResult<List<ChatMessageResponseDto>>> GetMessages([FromQuery] Guid sellerId, [FromQuery] Guid customerId)
        {
            if (sellerId == Guid.Empty || customerId == Guid.Empty)
                return BadRequest("SellerId e CustomerId sao obrigatorios.");

            var userId = User.GetUserId();
            if (User.IsInRole("Seller"))
            {
                var isOwner = await _context.Sellers
                    .AsNoTracking()
                    .AnyAsync(s => s.Id == sellerId && s.UserId == userId);

                if (!isOwner)
                    return Forbid();
            }
            else
            {
                if (userId != customerId)
                    return Forbid();
            }

            var messages = await _context.ChatMessages
                .AsNoTracking()
                .Where(m => m.SellerId == sellerId && m.CustomerId == customerId)
                .OrderBy(m => m.CreatedAt)
                .Select(m => new ChatMessageResponseDto
                {
                    Id = m.Id,
                    SellerId = m.SellerId,
                    CustomerId = m.CustomerId,
                    SenderUserId = m.SenderUserId,
                    Message = m.Message,
                    CreatedAt = m.CreatedAt
                })
                .ToListAsync();

            return Ok(messages);
        }

        [HttpPost("contact-requests")]
        public async Task<ActionResult> CreateContactRequest([FromBody] CreateContactRequestDto dto)
        {
            if (dto == null || dto.SellerUserId == Guid.Empty || string.IsNullOrWhiteSpace(dto.Message))
                return BadRequest("Dados invalidos.");

            var userId = User.GetUserId();
            if (User.IsInRole("Seller"))
                return Forbid();

            var seller = await _context.Sellers
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.UserId == dto.SellerUserId);

            if (seller == null)
                return NotFound("Vendedor nao encontrado.");

            var payload = $"Solicitacao de contato: {dto.Message.Trim()}";

            var chatMessage = new ChatMessage
            {
                SellerId = seller.Id,
                CustomerId = userId,
                SenderUserId = userId,
                Message = payload
            };

            _context.ChatMessages.Add(chatMessage);
            await _context.SaveChangesAsync();

            await _chatHub.Clients.Users(dto.SellerUserId.ToString(), userId.ToString())
                .SendAsync("ReceivePrivateMessage", userId.ToString(), payload);

            await _chatHub.Clients.User(dto.SellerUserId.ToString())
                .SendAsync("ReceiveNotification", "Nova solicitacao de contato", payload);

            return Ok();
        }

        [HttpPost("messages")]
        public async Task<ActionResult<ChatMessageResponseDto>> SendMessage([FromBody] SendChatMessageDto dto)
        {
            if (dto == null || dto.RecipientUserId == Guid.Empty || string.IsNullOrWhiteSpace(dto.Message))
                return BadRequest("Dados invalidos.");

            var senderId = User.GetUserId();
            var senderIsSeller = User.IsInRole("Seller");

            Guid sellerId;
            Guid customerId;
            Guid recipientUserId;

            if (senderIsSeller)
            {
                var seller = await _context.Sellers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.UserId == senderId);

                if (seller == null)
                    return NotFound("Vendedor nao encontrado.");

                sellerId = seller.Id;
                customerId = dto.RecipientUserId;
                recipientUserId = dto.RecipientUserId;
            }
            else
            {
                var seller = await _context.Sellers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.UserId == dto.RecipientUserId);

                if (seller == null)
                    return NotFound("Vendedor nao encontrado.");

                sellerId = seller.Id;
                customerId = senderId;
                recipientUserId = seller.UserId;
            }

            var payload = dto.Message.Trim();
            var chatMessage = new ChatMessage
            {
                SellerId = sellerId,
                CustomerId = customerId,
                SenderUserId = senderId,
                Message = payload
            };

            _context.ChatMessages.Add(chatMessage);
            await _context.SaveChangesAsync();

            await _chatHub.Clients.User(recipientUserId.ToString())
                .SendAsync("ReceivePrivateMessage", senderId.ToString(), payload);

            if (!senderIsSeller)
            {
                var title = payload.StartsWith("Solicitacao de contato:", StringComparison.OrdinalIgnoreCase)
                    ? "Nova solicitacao de contato"
                    : "Nova mensagem no chat";

                await _chatHub.Clients.User(recipientUserId.ToString())
                    .SendAsync("ReceiveNotification", title, payload);
            }

            return Ok(new ChatMessageResponseDto
            {
                Id = chatMessage.Id,
                SellerId = chatMessage.SellerId,
                CustomerId = chatMessage.CustomerId,
                SenderUserId = chatMessage.SenderUserId,
                Message = chatMessage.Message,
                CreatedAt = chatMessage.CreatedAt
            });
        }
    }
}
