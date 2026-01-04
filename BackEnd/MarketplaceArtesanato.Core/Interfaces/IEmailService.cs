using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketplaceArtesanato.Core.Interfaces
{
    public interface IEmailService
    {
        Task SendApprovalEmailAsync(string toEmail, string sellerName);
        Task SendPasswordResetEmailAsync(string toEmail, string userName, string resetLink);
    }
}
