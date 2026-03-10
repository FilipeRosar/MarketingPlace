using MarketplaceArtesanato.Core.Entities;

namespace MarketplaceArtesanato.Core.Interfaces;

public interface ICommissionCalculationService
{
    /// <summary>
    /// Calcula a comissão e taxa de serviço para uma venda
    /// </summary>
    /// <param name="orderTotal">Valor total do pedido</param>
    /// <param name="seller">Vendedor (para obter taxa de comissão)</param>
    /// <returns>Tupla contendo (comissão_do_vendedor, taxa_de_serviço, receita_plataforma)</returns>
    Task<(decimal SellerCommission, decimal ServiceFee, decimal PlatformRevenue)> CalculateFeesAsync(
        decimal orderTotal, 
        Seller seller);

    /// <summary>
    /// Retorna a percentagem de taxa de serviço (2.5% padrão)
    /// </summary>
    Task<decimal> GetServiceFeePercentageAsync();
}
