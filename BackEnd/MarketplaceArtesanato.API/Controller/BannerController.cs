using MarketplaceArtesanato.Core.Entities;
using MarketplaceArtesanato.Core.Entities.DTO;
using MarketplaceArtesanato.Core.Models.Requests;
using MarketplaceArtesanato.Services.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarketplaceArtesanato.API.Controllers
{
    [Route("api/banners")]
    [ApiController]
    public class BannersController : ControllerBase
    {
        private readonly IBannerService _bannerService;

        public BannersController(IBannerService bannerService)
        {
            _bannerService = bannerService;
        }

        [HttpGet]
        public async Task<ActionResult<List<Banner>>> GetActiveBanners()
        {
            var banners = await _bannerService.GetActiveBannersAsync();
            return Ok(banners);
        }

        [HttpGet("admin")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<List<Banner>>> GetAllBanners()
        {
            var banners = await _bannerService.GetAllBannersAsync();
            return Ok(banners);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CreateBanner([FromForm] CreateBannerDto dto)
        {
            var banner = await _bannerService.CreateBannerAsync(dto);
            return CreatedAtAction(nameof(GetActiveBanners), new { id = banner.Id }, banner);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdateBanner(Guid id, [FromForm] UpdateBannerDto dto)
        {
            try
            {
                await _bannerService.UpdateBannerAsync(id, dto);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteBanner(Guid id)
        {
            await _bannerService.DeleteBannerAsync(id);
            return NoContent();
        }
    }
}