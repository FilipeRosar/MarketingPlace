using AutoMapper;
using MarketplaceArtesanato.API.Models.Responses;
using MarketplaceArtesanato.Core.Entities;
using MarketplaceArtesanato.Core.Entities.Models.Requests;
using MarketplaceArtesanato.Core.Entities.Models.Responses;

namespace MarketplaceArtesanato.API.Mapping
{
    public class SellerProfile : Profile
    {
        public SellerProfile()
        {
            CreateMap<Seller, SellerResponseDto>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.StoreName))
                .ForMember(dest => dest.ProfileImageUrl, opt => opt.MapFrom(src => src.ProfileImageUrl))
                .ForMember(dest => dest.BannerImageUrl, opt => opt.MapFrom(src => src.BannerImageUrl))
                .ForMember(dest => dest.Moments, opt => opt.MapFrom(src => src.Moments))
                .ForMember(dest => dest.Instagram, opt => opt.MapFrom(src => src.InstagramUrl))
                .ForMember(dest => dest.Facebook, opt => opt.MapFrom(src => src.FacebookUrl))
                .ForMember(dest => dest.Tiktok, opt => opt.MapFrom(src => src.TiktokUrl))
                .ForMember(dest => dest.Youtube, opt => opt.MapFrom(src => src.YoutubeUrl));

            CreateMap<Moment, MomentResponseDto>();

            CreateMap<CreateMomentDto, Moment>();
        }
    }
}
