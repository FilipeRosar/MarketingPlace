using MailKit.Net.Smtp;
using MarketplaceArtesanato.Core.Events;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using System.Net.Mail;
using System.Text;
using SmtpClient = MailKit.Net.Smtp.SmtpClient;


namespace MarketplaceArtesanato.Services.Consumers
{
    public class OrderPaidConsumer : IConsumer<OrderPaidEvent>
    {
        private readonly IConfiguration _config;
        private readonly ILogger<OrderPaidConsumer> _logger;

        public OrderPaidConsumer(IConfiguration config, ILogger<OrderPaidConsumer> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<OrderPaidEvent> context)
        {
            var evt = context.Message;

            try
            {
                var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Templates", "order-confirmation.html");
                var htmlBody = await File.ReadAllTextAsync(templatePath);

                // Substitui dados do cliente (simulação — em produção use DbContext)
                var customerEmail = "cliente@exemplo.com";
                var customerName = "João Silva";

                // Monta lista de comissões
                var commissionsHtml = new StringBuilder();
                foreach (var (sellerId, commission) in evt.SellerCommissions)
                {
                    commissionsHtml.AppendLine($@"
                    <tr>
                        <td style='padding: 8px; border-bottom: 1px solid #eee;'>{sellerId}</td>
                        <td style='padding: 8px; border-bottom: 1px solid #eee; text-align: right;'>{commission:C}</td>
                    </tr>");
                }

                if (commissionsHtml.Length == 0)
                {
                    commissionsHtml.AppendLine("<tr><td colspan='2' style='text-align:center; color:#999;'>Nenhuma comissão calculada</td></tr>");
                }

                htmlBody = htmlBody
                    .Replace("{{OrderId}}", evt.OrderId.ToString())
                    .Replace("{{CustomerName}}", customerName)
                    .Replace("{{Total}}", evt.Total.ToString("C"))
                    .Replace("{{PaidAt}}", evt.PaidAt.ToString("dd/MM/yyyy HH:mm"))
                    .Replace("{{CommissionsTable}}", commissionsHtml.ToString());

                var email = new MimeMessage();
                email.From.Add(MailboxAddress.Parse(_config["Smtp:From"] ?? "no-reply@artesanato.com"));
                email.To.Add(MailboxAddress.Parse(customerEmail));
                email.Subject = $"Pedido #{evt.OrderId} Pago – Comissões Calculadas";
                email.Body = new TextPart("html") { Text = htmlBody };

                using var smtp = new SmtpClient();
                await smtp.ConnectAsync(_config["Smtp:Host"], int.Parse(_config["Smtp:Port"]), true);
                await smtp.AuthenticateAsync(_config["Smtp:User"], _config["Smtp:Pass"]);
                await smtp.SendAsync(email);
                await smtp.DisconnectAsync(true);

                _logger.LogInformation("Email com comissões enviado para {Email} | Pedido {OrderId}", customerEmail, evt.OrderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao enviar email de confirmação para pedido {OrderId}", evt.OrderId);
                throw;
            }
        }
    }
}
