using MarketplaceArtesanato.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketplaceArtesanato.Services.Services.Configuration
{
    public static class PricingServiceConfiguration
    {

        public static IServiceCollection AddPricingServices(this IServiceCollection services)
        {
            // Serviço principal de cálculo de preços
            services.AddScoped<IPriceCalculationService, PriceCalculationService>();

            // Registra todas as regras de preço em ordem de prioridade
            services.AddScoped<IPriceRule, ProductDiscountRule>();
            services.AddScoped<IPriceRule, PromotionRule>();
            services.AddScoped<IPriceRule, CampaignRule>();
            services.AddScoped<IPriceRule, LoyaltyDiscountRule>();

            // Adicione aqui novas regras de preço conforme necessário
            // services.AddScoped<IPriceRule, SeasonalDiscountRule>();
            // services.AddScoped<IPriceRule, BulkDiscountRule>();
            // services.AddScoped<IPriceRule, FirstTimeBuyerRule>();

            return services;
        }
    }
}
