using MarketplaceArtesanato.Core.Entities;
using MarketplaceArtesanato.Core.Entities.DTO;
using MarketplaceArtesanato.Core.Interfaces;
using MarketplaceArtesanato.Data.Data;
using Microsoft.EntityFrameworkCore;

namespace MarketplaceArtesanato.Services.Services
{
    public interface IBannerService
    {
        Task<List<Banner>> GetActiveBannersAsync();
        Task<List<Banner>> GetAllBannersAsync(); // Para o Admin ver inativos também
        Task<Banner> CreateBannerAsync(CreateBannerDto dto);
        Task UpdateBannerAsync(Guid id, UpdateBannerDto dto);
        Task DeleteBannerAsync(Guid id);
    }

    public class BannerService : IBannerService
    {
        private readonly ArtesianDbContext _context;
        private readonly IStorageService _storage;

        public BannerService(ArtesianDbContext context, IStorageService storage)
        {
            _context = context;
            _storage = storage;
        }

        public async Task<List<Banner>> GetActiveBannersAsync()
        {
            return await _context.Banners
                .Where(b => !b.IsDeleted && b.IsActive)
                .OrderBy(b => b.DisplayOrder)
                .ToListAsync();
        }

        public async Task<List<Banner>> GetAllBannersAsync()
        {
            return await _context.Banners
                .Where(b => !b.IsDeleted)
                .OrderBy(b => b.DisplayOrder)
                .ToListAsync();
        }

        public async Task<Banner> CreateBannerAsync(CreateBannerDto dto)
        {
            var imageUrl = await _storage.UploadFileAsync(dto.Image);

            var banner = new Banner
            {
                Id = Guid.NewGuid(),
                Title = dto.Title,
                Subtitle = dto.Subtitle,
                LinkUrl = dto.LinkUrl,
                DisplayOrder = dto.DisplayOrder,
                ImageUrl = imageUrl,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            _context.Banners.Add(banner);
            await _context.SaveChangesAsync();
            return banner;
        }

        public async Task UpdateBannerAsync(Guid id, UpdateBannerDto dto)
        {
            var banner = await _context.Banners.FindAsync(id);
            if (banner == null || banner.IsDeleted) throw new KeyNotFoundException("Banner não encontrado.");

            if (dto.Title != null) banner.Title = dto.Title;
            if (dto.Subtitle != null) banner.Subtitle = dto.Subtitle;
            if (dto.LinkUrl != null) banner.LinkUrl = dto.LinkUrl;
            if (dto.IsActive.HasValue) banner.IsActive = dto.IsActive.Value;
            if (dto.DisplayOrder.HasValue) banner.DisplayOrder = dto.DisplayOrder.Value;

            if (dto.Image != null)
            {
                try { await _storage.DeleteAsync(banner.ImageUrl); } catch { }
                banner.ImageUrl = await _storage.UploadFileAsync(dto.Image);
            }

            banner.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteBannerAsync(Guid id)
        {
            var banner = await _context.Banners.FindAsync(id);
            if (banner != null)
            {
                banner.IsDeleted = true; 
                await _context.SaveChangesAsync();
            }
        }
    }
}