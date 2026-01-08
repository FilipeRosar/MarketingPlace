using MarketplaceArtesanato.API.Models.Responses;
using MarketplaceArtesanato.Core.Entities.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketplaceArtesanato.Core.Interfaces
{
    public interface IProductService
    {
        Task<PaginatedResult<ProductResponseDto>> GetAllAsync(int page, int pageSize, string? search, string? subcategory, int? category, decimal? minPrice, decimal? maxPrice, Guid? sellerId);
        Task<ProductResponseDto?> GetByIdAsync(Guid id);
        Task<ProductResponseDto> CreateAsync(Guid sellerId, CreateProductDto dto);
        Task<bool> UpdateAsync(Guid id, Guid userId, string userRole, UpdateProductDto dto);
        Task<bool> DeleteAsync(Guid id, Guid userId, string userRole);
    }
    public class PaginatedResult<T>
    {
        public List<T> Data { get; set; } = new();
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Pages { get; set; }
    }
}
