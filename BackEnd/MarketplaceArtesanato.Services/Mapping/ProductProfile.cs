using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AutoMapper;
using MarketplaceArtesanato.Core.Entities;
using MarketplaceArtesanato.Core.Entities.Enums;
using MarketplaceArtesanato.API.Models;
using MarketplaceArtesanato.API.Models.Responses;

namespace MarketplaceArtesanato.Services.Mapping
{
    public class ProductProfile : Profile
    {
        public ProductProfile()
        {
            CreateMap<Product, ProductResponseDto>()
                .ForMember(dest => dest.Category, opt => opt.MapFrom(src => (ProductCategory)src.Category))
                .ForMember(dest => dest.Seller, opt => opt.MapFrom(src => src.Seller));

            CreateMap<User, SellerResponseDto>()
           .ForMember(dest => dest.Role, opt => opt.MapFrom(src => (int)src.Role));

            CreateMap<Address, AddressResponseDto>();
            CreateMap<Ratings, RatingResponseDto>();
        }
    }
}
