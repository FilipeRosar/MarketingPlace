using AutoMapper;
using MarketplaceArtesanato.API.Models;
using MarketplaceArtesanato.API.Models.Responses;
using MarketplaceArtesanato.Core.Entities;
using MarketplaceArtesanato.Core.Entities.DTO;
using MarketplaceArtesanato.Core.Entities.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MarketplaceArtesanato.API.Mapping
{
    public class ProductProfile : Profile
    {
        public ProductProfile()
        {
            CreateMap<CreateProductDto, Product>()
                .ForMember(dest => dest.Images, opt => opt.Ignore())
                .ForMember(dest => dest.StoryMedia, opt => opt.Ignore())
                .ForMember(dest => dest.Tags, opt => opt.MapFrom(src => string.Join(",", src.Tags)));

            CreateMap<Address, AddressResponseDto>();
            CreateMap<ProductImage, ProductImageDto>();
            CreateMap<UpdateProductDto, Product>()
                .ForMember(dest => dest.BoostedUntil, opt => opt.Ignore())
                .ForMember(dest => dest.SalePrice, opt => opt.Ignore())
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<Seller, SellerResponseDto>()
                .ForMember(dest => dest.HasVerifiedBadge, opt => opt.MapFrom(src =>
                    src.Subscription != null && src.Subscription.IsActive && src.Subscription.HasVerifiedBadge));

            CreateMap<Product, ProductResponseDto>()
                .ForMember(dest => dest.Seller, opt => opt.MapFrom(src => src.Seller))
                .ForMember(dest => dest.SellerName, opt => opt.MapFrom(src => src.Seller!.StoreName))
                .ForMember(dest => dest.IsBoosted, opt => opt.MapFrom(src =>
                    src.BoostedUntil.HasValue && src.BoostedUntil.Value > DateTime.UtcNow))
                .ForMember(dest => dest.AverageRating, opt => opt.MapFrom(src =>
                    src.Ratings != null && src.Ratings.Any() ? src.Ratings.Average(r => r.Stars) : 5.0))
                .ForMember(dest => dest.TotalRatings, opt => opt.MapFrom(src =>
                    src.Ratings != null ? src.Ratings.Count : 0))
                .ForMember(dest => dest.Tags, apt => apt.MapFrom(src =>
                string.IsNullOrEmpty(src.Tags) ? new List<string>() : src.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()))
                .ForMember(dest => dest.StoryMediaUrls, opt => opt.MapFrom(src =>
                    src.StoryMedia != null ? src.StoryMedia.Select(m => m.Url).ToList() : new List<string>()));
        }
    }
}
