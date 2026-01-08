using MarketplaceArtesanato.API.Models.Responses; 
using MarketplaceArtesanato.Core.Entities.DTO;
using MarketplaceArtesanato.Core.Entities.DTO;
using MarketplaceArtesanato.Core.Entities.Models.Requests;
using MarketplaceArtesanato.Core.Entities.Models.Responses;
using MarketplaceArtesanato.Core.Events;
using MarketplaceArtesanato.Core.Models.Requests;

namespace MarketplaceArtesanato.Core.Interfaces
{
    public interface IOrderService
    {
        Task<List<OrderResponseDto>> GetByUserAsync(Guid userId, string role);
        Task<OrderResponseDto> GetByIdAsync(Guid orderId, Guid userId, string role);
        Task<CheckoutResponseResult> CreateOrderAsync(Guid buyerId, CheckoutRequestDto dto);
        Task UpdateTrackingAsync(Guid orderId, Guid userId, string role, string trackingCode);
        Task CancelOrderAsync(Guid orderId, Guid userId, string role);
    }

    public class CheckoutResponseResult
    {
        public Guid OrderId { get; set; }
        public string PaymentUrl { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}
