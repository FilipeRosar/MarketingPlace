using MarketplaceArtesanato.Core.Entities;
using MarketplaceArtesanato.Core.Interfaces;

namespace MarketplaceArtesanato.Services.Services;

public class CommissionCalculationService : ICommissionCalculationService
{
    private readonly ISellerSubscriptionService _subscriptionService;
    private readonly IPlatformFeeService _platformFeeService;

    private const decimal DEFAULT_SERVICE_FEE_PERCENTAGE = 0.025m; // 2.5%

    public CommissionCalculationService(
        ISellerSubscriptionService subscriptionService,
        IPlatformFeeService platformFeeService)
    {
        _subscriptionService = subscriptionService;
        _platformFeeService = platformFeeService;
    }

    public async Task<(decimal SellerCommission, decimal ServiceFee, decimal PlatformRevenue)> CalculateFeesAsync(
        decimal orderTotal, 
        Seller seller)
    {
        if (seller == null)
            throw new ArgumentNullException(nameof(seller));

        if (orderTotal <= 0)
            throw new ArgumentException("Order total must be positive", nameof(orderTotal));

        // Obter taxa de comissão do vendedor baseado no plano
        decimal commissionPercentage = await _platformFeeService.GetCommissionRateAsync(seller.Id) / 100m;
        decimal sellerCommission = orderTotal * commissionPercentage;

        // Taxa de serviço configurada (padrão 2.5%)
        decimal serviceFeePercentage = await GetServiceFeePercentageAsync();
        decimal serviceFee = orderTotal * serviceFeePercentage;

        // Receita da plataforma = taxa de serviço + comissão do vendedor
        decimal platformRevenue = serviceFee + sellerCommission;

        return (sellerCommission, serviceFee, platformRevenue);
    }

    public async Task<decimal> GetServiceFeePercentageAsync()
    {
        return DEFAULT_SERVICE_FEE_PERCENTAGE;
    }
}
