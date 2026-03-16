using MarketplaceArtesanato.Core.Entities;
using MarketplaceArtesanato.Core.Entities.Enums;
using MarketplaceArtesanato.Data.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace MarketplaceArtesanato.API.Authorization
{
    public class SellerPlanRequirement : IAuthorizationRequirement
    {
        public SellerPlanRequirement(SellerPlan requiredPlan = SellerPlan.Pro)
        {
            RequiredPlan = requiredPlan;
        }

        public SellerPlan RequiredPlan { get; }
    }

    public class SellerPlanRequirementHandler : AuthorizationHandler<SellerPlanRequirement>
    {
        private readonly ArtesianDbContext _context;
        private readonly ILogger<SellerPlanRequirementHandler> _logger;

        public SellerPlanRequirementHandler(ArtesianDbContext context, ILogger<SellerPlanRequirementHandler> logger)
        {
            _context = context;
            _logger = logger;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context, 
            SellerPlanRequirement requirement)
        {
            try
            {
                // Verifica se usuário está autenticado
                var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)
                    ?? context.User.FindFirst("sub")
                    ?? context.User.FindFirst("id");

                if (userIdClaim == null)
                {
                    _logger.LogWarning("Usuário sem claim de ID tentou acessar analytics avançado");
                    context.Fail();
                    return;
                }

                if (!Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    _logger.LogWarning("ID de usuário inválido: {UserId}", userIdClaim.Value);
                    context.Fail();
                    return;
                }

                var seller = await _context.Sellers
                    .FirstOrDefaultAsync(s => s.UserId == userId && s.IsDeleted == false);

                if (seller == null)
                {
                    _logger.LogWarning("Usuário {UserId} não é vendedor", userId);
                    context.Fail();
                    return;
                }

                if (!seller.IsApproved || !seller.StoreApproved)
                {
                    _logger.LogWarning("Vendedor {SellerId} não está aprovado", seller.Id);
                    context.Fail();
                    return;
                }

                var subscription = await _context.SellerSubscriptions
                    .FirstOrDefaultAsync(s => s.SellerId == seller.Id && s.IsDeleted == false);

                if (subscription == null)
                {
                    _logger.LogWarning("Vendedor {SellerId} sem subscription ativa", seller.Id);
                    context.Fail();
                    return;
                }

                if (!subscription.IsActive)
                {
                    _logger.LogWarning("Subscription de vendedor {SellerId} não está ativa", seller.Id);
                    context.Fail();
                    return;
                }

                if (subscription.ExpiresAt.HasValue && subscription.ExpiresAt.Value < DateTime.UtcNow)
                {
                    _logger.LogWarning("Subscription de vendedor {SellerId} expirou em {ExpiresAt}", 
                        seller.Id, subscription.ExpiresAt);
                    context.Fail();
                    return;
                }

                if (!subscription.HasAdvancedAnalytics)
                {
                    _logger.LogWarning("Vendedor {SellerId} com plano {Plan} não tem acesso a analytics avançado", 
                        seller.Id, subscription.Plan);
                    context.Fail();
                    return;
                }

                if ((int)subscription.Plan < (int)requirement.RequiredPlan)
                {
                    _logger.LogWarning("Vendedor {SellerId} com plano {CurrentPlan} requer {RequiredPlan}", 
                        seller.Id, subscription.Plan, requirement.RequiredPlan);
                    context.Fail();
                    return;
                }

                _logger.LogInformation("Vendedor {SellerId} autorizado com plano {Plan}", 
                    seller.Id, subscription.Plan);

                var claimsIdentity = context.User.Identity as System.Security.Claims.ClaimsIdentity;
                claimsIdentity?.AddClaim(new Claim("seller_plan", subscription.Plan.ToString()));
                claimsIdentity?.AddClaim(new Claim("seller_id", seller.Id.ToString()));

                context.Succeed(requirement);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao validar SellerPlanRequirement");
                context.Fail();
            }
        }
    }
}
