using MarketplaceArtesanato.Core.Entities.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketplaceArtesanato.Core.Interfaces
{
    public interface ICartService
    {
        Task<CartDto> GetCartAsync(Guid userId, string? couponCode = null);
        Task AddItemAsync(Guid customerId, Guid productId, int quantity = 1);
        Task UpdateItemQuantityAsync(Guid customerId, Guid productId, int quantity);
        Task RemoveItemAsync(Guid customerId, Guid productId);
        Task ClearCartAsync(Guid customerId);
    }
}
