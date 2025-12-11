using AutoMapper;
using MarketplaceArtesanato.Core.Entities;
using MarketplaceArtesanato.Core.Entities.Models.Responses;

namespace MarketplaceArtesanato.API.Mapping
{
    public class OrderProfile : Profile
    {
        public OrderProfile()
        {
            CreateMap<Order, OrderResponseDto>()
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
                .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.Items ?? new List<OrderItem>()));

            CreateMap<OrderItem, OrderItemResponseDto>()
                .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src =>
                    src.Product != null ? src.Product.Name : "Produto Removido"))

                .ForMember(dest => dest.ProductImage, opt => opt.MapFrom(src =>
                    src.Product != null && src.Product.Images != null && src.Product.Images.Any()
                    ? src.Product.Images.FirstOrDefault()
                    : null));
        }
    }
}

