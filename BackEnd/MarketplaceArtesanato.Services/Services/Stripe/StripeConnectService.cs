using System;
using MarketplaceArtesanato.Core.Entities.Models.Responses;
using MarketplaceArtesanato.Core.Interfaces;
using MarketplaceArtesanato.Data.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Stripe;

namespace MarketplaceArtesanato.Services.Services.Stripe
{
    public class StripeConnectService : IStripeConnectService
    {
        private readonly ArtesianDbContext _context;
        private readonly IConfiguration _config;

        public StripeConnectService(ArtesianDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        public async Task<string> CreateOnboardingLinkAsync(Guid userId)
        {
            var seller = await _context.Sellers
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (seller == null)
                throw new InvalidOperationException("Vendedor nao encontrado.");

            if (string.IsNullOrWhiteSpace(seller.StripeAccountId))
            {
                var accountService = new AccountService();
                var account = await accountService.CreateAsync(new AccountCreateOptions
                {
                    Type = "standard",
                    Email = seller.User?.Email,
                    BusinessType = "individual"
                });

                seller.StripeAccountId = account.Id;
                seller.IsStripeConnected = false;
                await _context.SaveChangesAsync();
            }

            var returnUrl = _config["Stripe:ConnectReturnUrl"]
                ?? _config["Stripe:SuccessUrl"]
                ?? "http://localhost:4200/seller-dashboard";
            var refreshUrl = _config["Stripe:ConnectRefreshUrl"] ?? returnUrl;

            var linkService = new AccountLinkService();
            var link = await linkService.CreateAsync(new AccountLinkCreateOptions
            {
                Account = seller.StripeAccountId,
                RefreshUrl = refreshUrl,
                ReturnUrl = returnUrl,
                Type = "account_onboarding"
            });

            return link.Url;
        }

        public async Task<string> CreateDashboardLinkAsync(Guid userId)
        {
            var seller = await _context.Sellers
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (seller == null)
                throw new InvalidOperationException("Vendedor nao encontrado.");

            if (string.IsNullOrWhiteSpace(seller.StripeAccountId))
                throw new InvalidOperationException("Conta Stripe nao vinculada.");

            var loginService = new AccountLoginLinkService();
            var link = await loginService.CreateAsync(seller.StripeAccountId, new AccountLoginLinkCreateOptions());

            return link.Url;
        }

        public async Task<StripeConnectStatusDto> GetStatusAsync(Guid userId)
        {
            var seller = await _context.Sellers
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (seller == null)
                throw new InvalidOperationException("Vendedor nao encontrado.");

            if (string.IsNullOrWhiteSpace(seller.StripeAccountId))
            {
                return new StripeConnectStatusDto
                {
                    IsConnected = false
                };
            }

            var accountService = new AccountService();
            var account = await accountService.GetAsync(seller.StripeAccountId);

            var isConnected = account.ChargesEnabled && account.DetailsSubmitted;
            if (seller.IsStripeConnected != isConnected)
            {
                seller.IsStripeConnected = isConnected;
                await _context.SaveChangesAsync();
            }

            return new StripeConnectStatusDto
            {
                IsConnected = isConnected,
                AccountId = seller.StripeAccountId,
                ChargesEnabled = account.ChargesEnabled,
                DetailsSubmitted = account.DetailsSubmitted
            };
        }
    }
}
