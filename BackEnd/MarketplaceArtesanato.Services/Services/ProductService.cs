using AutoMapper;
using MarketplaceArtesanato.API.Models.Responses;
using MarketplaceArtesanato.Core.Entities;
using MarketplaceArtesanato.Core.Entities.DTO;
using MarketplaceArtesanato.Core.Interfaces;
using MarketplaceArtesanato.Data.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketplaceArtesanato.Services.Services
{
    public class ProductService : IProductService
    {
        private readonly ArtesianDbContext _context;
        private readonly IMapper _mapper;
        private readonly IStorageService _storage;

        public ProductService(ArtesianDbContext context, IMapper mapper, IStorageService storage)
        {
            _context = context;
            _mapper = mapper;
            _storage = storage;
        }

        public async Task<PaginatedResult<ProductResponseDto>> GetAllAsync(int page, int pageSize, string? search, int? category, decimal? minPrice, decimal? maxPrice, Guid? sellerId)
        {
            var query = _context.Products
                .Include(p => p.Seller!).ThenInclude(s => s.Address)
                .Include(p => p.Ratings!).ThenInclude(r => r.Customer)
                .Include(p => p.Images)
                .Where(p => !p.IsDeleted)
                .AsQueryable();

            // Filtros
            if (sellerId.HasValue)
                query = query.Where(p => p.SellerId == sellerId.Value);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var lowerSearch = search.Trim().ToLower();
                query = query.Where(p =>
                    p.Name.ToLower().Contains(lowerSearch) ||
                    (p.Description != null && p.Description.ToLower().Contains(lowerSearch)) ||
                    p.Seller!.StoreName.ToLower().Contains(lowerSearch));
            }

            if (category.HasValue)
                query = query.Where(p => (int)p.Category == category.Value);

            if (minPrice.HasValue)
                query = query.Where(p => p.Price >= minPrice.Value);

            if (maxPrice.HasValue)
                query = query.Where(p => p.Price <= maxPrice.Value);

            // Paginação
            var total = await query.CountAsync();
            var products = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var dtos = _mapper.Map<List<ProductResponseDto>>(products);

            return new PaginatedResult<ProductResponseDto>
            {
                Data = dtos,
                Total = total,
                Page = page,
                PageSize = pageSize,
                Pages = (int)Math.Ceiling(total / (double)pageSize)
            };
        }

        public async Task<ProductResponseDto?> GetByIdAsync(Guid id)
        {
            var product = await _context.Products
                .Include(p => p.Seller!).ThenInclude(s => s.Address)
                .Include(p => p.Ratings!).ThenInclude(r => r.Customer)
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return null;

            return _mapper.Map<ProductResponseDto>(product);
        }

        public async Task<ProductResponseDto> CreateAsync(Guid sellerId, CreateProductDto dto)
        {
            var seller = await _context.Sellers.FirstOrDefaultAsync(s => s.UserId == sellerId);
            if (seller == null) throw new KeyNotFoundException("Vendedor não encontrado.");

            if (!seller.IsApproved)
                throw new UnauthorizedAccessException("Sua loja ainda está em análise. Aguarde a aprovação para publicar produtos.");

            // Upload de Imagens
            var imageUrls = new List<string>();
            if (dto.Images != null)
            {
                foreach (var file in dto.Images)
                {
                    if (file.Length > 0 && IsImage(file))
                    {
                        var url = await _storage.UploadFileAsync(file);
                        imageUrls.Add(url);
                    }
                }
            }

            var product = _mapper.Map<Product>(dto);
            product.Id = Guid.NewGuid();
            product.SellerId = seller.Id;
            // product.Seller = seller; // Redundant if SellerId is set, but harmless.

            product.Images = imageUrls.Select(url => new ProductImage
            {
                Id = Guid.NewGuid(),
                Url = url,
                ProductId = product.Id,
                IsMain = imageUrls.First() == url
            }).ToList();

            product.CreatedAt = DateTime.UtcNow;
            product.IsDeleted = false;

            if (dto.SalePrice.HasValue && dto.SalePrice > 0)
            {
                product.SalePrice = dto.SalePrice.Value;
            }

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            // To return the full DTO with Seller info, we might need to load it explicitly or map carefully
            // Since 'product.Seller' might be null in the returned entity if not explicitly set or loaded.
            product.Seller = seller;

            return _mapper.Map<ProductResponseDto>(product);
        }

        public async Task<bool> UpdateAsync(Guid id, Guid userId, string userRole, UpdateProductDto dto)
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id);
            if (product == null) return false; // Not Found

            // Verificação de permissão
            if (product.SellerId != userId && userRole != "Admin")
                throw new UnauthorizedAccessException("Sem permissão para editar este produto.");

            _mapper.Map(dto, product);
            product.SalePrice = dto.SalePrice;
            product.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(Guid id, Guid userId, string userRole)
        {
            var product = await _context.Products
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return false;

            if (product.SellerId != userId && userRole != "Admin")
                throw new UnauthorizedAccessException("Sem permissão para deletar este produto.");

            // Remove imagens do storage
            foreach (var image in product.Images)
            {
                try { await _storage.DeleteAsync(image.Url); } catch { }
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            return true;
        }

        private bool IsImage(IFormFile file)
        {
            try
            {
                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                return new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" }.Contains(ext);
            }
            catch { return false; }
        }
    }
}
