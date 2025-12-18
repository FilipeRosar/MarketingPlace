using MarketplaceArtesanato.Core.Entities;
using Microsoft.Extensions.Configuration;
using Stripe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketplaceArtesanato.Services.Services.Stripe
{
    public class StripeAccountService
    {
        private readonly IConfiguration _config;

        public StripeAccountService(IConfiguration config)
        {
            _config = config;
            StripeConfiguration.ApiKey = _config["Stripe:SecretKey"];
        }

        public async Task<string> CreateOnboardingLinkAsync(Seller seller)
        {
            var accountService = new AccountService();
            var linkService = new AccountLinkService();

            if (string.IsNullOrEmpty(seller.StripeAccountId))
            {
                var accountOptions = new AccountCreateOptions
                {
                    Type = "express", 
                    Country = "BR",
                    Email = seller.User.Email,
                    Capabilities = new AccountCapabilitiesOptions
                    {
                        CardPayments = new AccountCapabilitiesCardPaymentsOptions { Requested = true },
                        Transfers = new AccountCapabilitiesTransfersOptions { Requested = true },
                    },
                };
                var account = await accountService.CreateAsync(accountOptions);
                seller.StripeAccountId = account.Id;
            }

            var linkOptions = new AccountLinkCreateOptions
            {
                Account = seller.StripeAccountId,
                RefreshUrl = $"{_config["AppUrl"]}/seller/onboarding/retry",
                ReturnUrl = $"{_config["AppUrl"]}/seller/onboarding/success",
                Type = "account_onboarding",
            };

            var link = await linkService.CreateAsync(linkOptions);
            return link.Url;
        }
    }
}
