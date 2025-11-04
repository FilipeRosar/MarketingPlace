using AutoMapper;
using MarketplaceArtesanato.API.Models;
using MarketplaceArtesanato.API.Models.Responses;
using MarketplaceArtesanato.Core.Entities;
using MarketplaceArtesanato.Core.Entities.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MarketplaceArtesanato.Services.Mapping
{
    public class ProductProfile : Profile
    {
        public ProductProfile()
        {
            IMappingExpression<Product, ProductResponseDto> mappingExpression = CreateMap<Product, ProductResponseDto>()
                .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.Category))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
                .ForMember(dest => dest.AverageRating, opt => opt.MapFrom(src =>
                    src.Ratings.Any() ? src.Ratings.Average(r => r.Stars) : 0))
                .ForMember(dest => dest.TotalRatings, opt => opt.MapFrom(src => src.Ratings.Count))
                .ForMember(dest => dest.Seller, opt => opt.MapFrom(src => src.Seller));

            CreateMap<Seller, SellerResponseDto>()
                .ForMember(dest => dest.CPF, opt => opt.MapFrom(src => src.CPF ?? "N/A"))
                .ForMember(dest => dest.CNPJ, opt => opt.MapFrom(src => src.CNPJ ?? "N/A"));

            CreateMap<Address, AddressResponseDto>();

            CreateMap<Rating, RatingResponseDto>()
                .ForMember(dest => dest.CustomerName, opt => opt.MapFrom(src => src.Customer.Name));
        }
    }
}
