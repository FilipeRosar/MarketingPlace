using MarketplaceArtesanato.Core.Entities.DTO;
using MarketplaceArtesanato.Core.Entities.Models.Requests;
using MarketplaceArtesanato.Core.Events;
using MarketplaceArtesanato.Core.Models.Requests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketplaceArtesanato.Core.Interfaces
{
    public interface IOrderService
    {
        Task<OrderDto> CreateFromCartAsync(Guid customerId, CheckoutRequestDto dto);
        Task<bool> ProcessPaymentAsync(PaymentProcessedEvent evt);
        Task<OrderDto?> GetByIdAsync(Guid orderId, Guid userId);
        Task<IEnumerable<OrderDto>> GetByCustomerAsync(Guid customerId);
        Task<IEnumerable<OrderDto>> GetBySellerAsync(Guid sellerId);
    }
}
