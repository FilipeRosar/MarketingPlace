using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketplaceArtesanato.Core.Interfaces
{
    public interface IFavoritesService
    {
        Task<List<Guid>> GetFavoriteProductIdsAsync(Guid userId);
        Task AddToFavoritesAsync(Guid userId, Guid productId);
        Task RemoveFromFavoritesAsync(Guid userId, Guid productId);
    }
}
