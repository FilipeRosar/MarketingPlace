using MarketplaceArtesanato.Core.Entities;
using MarketplaceArtesanato.Core.Entities.Enums;
using MarketplaceArtesanato.Core.Interfaces;
using MarketplaceArtesanato.Data.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Stripe.Checkout;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketplaceArtesanato.Services.Services
{
    public class SellerSubscribeService : ISellerSubscriptionService
    {
        private readonly ArtesianDbContext _context;
        private readonly IConfiguration _config;
        private readonly SessionService _sessionService;
        private readonly ILogger<SellerSubscribeService> _logger;

        public SellerSubscribeService(
            ArtesianDbContext context,
            IConfiguration config,
            SessionService sessionService,
            ILogger<SellerSubscribeService> logger)
        {
            _context = context;
            _config = config;
            _sessionService = sessionService;
            _logger = logger;
        }

        public Task CancelAsync(Guid sellerId)
        {
            var currentSubscription = _context.SellerSubscriptions
                .FirstOrDefault(sub => sub.SellerId == sellerId && sub.IsActive);
            if (currentSubscription == null)
            {
                throw new InvalidOperationException("Vendedor não possui plano ativo.");
            }
            currentSubscription.IsActive = false;
            currentSubscription.ExpiresAt = DateTime.UtcNow;
            return _context.SaveChangesAsync();
        }

        public async Task<SellerSubscription> ChangePlanAsync(Guid sellerId, SellerPlan newPlan)
        {
            var currentSubscription = _context.SellerSubscriptions
                .FirstOrDefault(sub => sub.SellerId == sellerId && sub.IsActive);
            if (currentSubscription == null)
            {
                throw new InvalidOperationException("Vendedor não possui plano ativo.");
            }
            if (currentSubscription.Plan == newPlan)
            {
                throw new InvalidOperationException("O vendedor já está inscrito neste plano.");
            }
            currentSubscription.IsActive = false;
            currentSubscription.ExpiresAt = DateTime.UtcNow;

            var newSubscription = CreateSubscription(sellerId, newPlan);

            _context.SellerSubscriptions.Add(newSubscription);
            await _context.SaveChangesAsync();
            return newSubscription;
        }

        public async Task<SellerSubscription> GetActiveSubscriptionAsync(Guid sellerId)
        {
            return await _context.SellerSubscriptions
                .AsNoTracking()
                .FirstOrDefaultAsync(sub => sub.SellerId == sellerId && sub.IsActive);
        }

        public async Task<SellerSubscription> SubscribeAsync(Guid sellerId, SellerPlan plan)
        {
            _logger.LogInformation("[SubscribeAsync] Iniciando para SellerId={SellerId}, Plan={Plan}", sellerId, plan);

            try
            {
                // Verifica se o seller existe
                var seller = await _context.Sellers
                    .FirstOrDefaultAsync(s => s.Id == sellerId);

                if (seller == null)
                {
                    _logger.LogError("[SubscribeAsync] Seller {SellerId} não encontrado", sellerId);
                    throw new InvalidOperationException($"Seller {sellerId} não encontrado");
                }

                _logger.LogInformation("[SubscribeAsync] Seller encontrado: {SellerName}", seller.StoreName);

                // Desativa assinatura atual se existir
                var current = await _context.SellerSubscriptions
                    .FirstOrDefaultAsync(sub => sub.SellerId == sellerId && sub.IsActive);

                if (current != null)
                {
                    _logger.LogInformation("[SubscribeAsync] Desativando assinatura atual: {CurrentPlan}", current.Plan);
                    current.IsActive = false;
                    current.ExpiresAt = DateTime.UtcNow;
                }

                // Cria nova assinatura
                var newSubscription = CreateSubscription(sellerId, plan);
                _context.SellerSubscriptions.Add(newSubscription);

                await _context.SaveChangesAsync();

                _logger.LogInformation("[SubscribeAsync] Assinatura criada com sucesso: Id={Id}, Plan={Plan}",
                    newSubscription.Id, newSubscription.Plan);

                return newSubscription;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SubscribeAsync] Erro ao processar assinatura para SellerId={SellerId}", sellerId);
                throw;
            }
        }

        public async Task<string> CreateCheckoutSessionAsync(Guid sellerId, SellerPlan plan)
        {
            var current = await _context.SellerSubscriptions
                .AsNoTracking()
                .FirstOrDefaultAsync(sub => sub.SellerId == sellerId && sub.IsActive);

            if (current != null && current.Plan == plan)
            {
                throw new InvalidOperationException("O vendedor ja esta inscrito neste plano.");
            }

            var preview = CreateSubscription(sellerId, plan);
            if (preview.MonthlyPrice <= 0m)
            {
                // Plano gratuito - ativa direto
                return string.Empty;
            }

            var successUrl = _config["Stripe:SellerSubscribeSuccessUrl"]
                ?? _config["Stripe:SuccessUrl"]
                ?? "http://localhost:4200/seller-dashboard?subscription=success";

            var cancelUrl = _config["Stripe:SellerSubscribeCancelUrl"]
                ?? _config["Stripe:CancelUrl"]
                ?? "http://localhost:4200/seller-dashboard?subscription=cancel";

            var amount = (long)Math.Round(preview.MonthlyPrice * 100m);

            var options = new SessionCreateOptions
            {
                Mode = "subscription",
                PaymentMethodTypes = new List<string> { "card" },
                SuccessUrl = successUrl,
                CancelUrl = cancelUrl,
                ClientReferenceId = sellerId.ToString(),
                Metadata = new Dictionary<string, string>
                {
                    { "SellerId", sellerId.ToString() },
                    { "SellerPlan", plan.ToString() }
                },
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        Quantity = 1,
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = "brl",
                            UnitAmount = amount,
                            Recurring = new SessionLineItemPriceDataRecurringOptions
                            {
                                Interval = "month"
                            },
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = $"Plano {plan}",
                                Description = BuildPlanDescription(preview)
                            }
                        }
                    }
                }
            };

            var session = await _sessionService.CreateAsync(options);

            _logger.LogInformation("[CreateCheckoutSession] Sessão criada para SellerId={SellerId}, Plan={Plan}, SessionId={SessionId}",
                sellerId, plan, session.Id);

            return session.Url;
        }

        private static SellerSubscription CreateSubscription(Guid sellerId, SellerPlan plan)
        {
            return plan switch
            {
                SellerPlan.Basic => new SellerSubscription
                {
                    SellerId = sellerId,
                    Plan = SellerPlan.Basic,
                    StartedAt = DateTime.UtcNow,
                    IsActive = true,
                    CommissionRate = 0.12m,
                    CanHighlightProducts = false,
                    MonthlyPrice = 0m,
                    HighlightLimit = 0,
                    HasVerifiedBadge = false,
                    HasAdvancedAnalytics = false,
                    HasPrioritySupport = false
                },
                SellerPlan.Pro => new SellerSubscription
                {
                    SellerId = sellerId,
                    Plan = SellerPlan.Pro,
                    StartedAt = DateTime.UtcNow,
                    IsActive = true,
                    CommissionRate = 0.09m,
                    CanHighlightProducts = true,
                    MonthlyPrice = 29.99m,
                    HighlightLimit = 8,
                    HasVerifiedBadge = true,
                    HasAdvancedAnalytics = true,
                    HasPrioritySupport = false
                },
                SellerPlan.Premium => new SellerSubscription
                {
                    SellerId = sellerId,
                    Plan = SellerPlan.Premium,
                    StartedAt = DateTime.UtcNow,
                    IsActive = true,
                    CommissionRate = 0.05m,
                    CanHighlightProducts = true,
                    MonthlyPrice = 59.90m,
                    HighlightLimit = 15,
                    HasVerifiedBadge = true,
                    HasAdvancedAnalytics = true,
                    HasPrioritySupport = true
                },
                _ => throw new ArgumentOutOfRangeException(nameof(plan), "Invalid seller plan")
            };
        }

        private static string BuildPlanDescription(SellerSubscription preview)
        {
            var highlights = preview.CanHighlightProducts ? $"Destaques: {preview.HighlightLimit}" : "Sem destaque";
            var badge = preview.HasVerifiedBadge ? "Selo verificado" : "Sem selo";
            var analytics = preview.HasAdvancedAnalytics ? "Analytics avancado" : "Analytics basico";
            var support = preview.HasPrioritySupport ? "Suporte prioritario" : "Suporte padrao";
            return $"{highlights} - {badge} - {analytics} - {support}";
        }
    }
}