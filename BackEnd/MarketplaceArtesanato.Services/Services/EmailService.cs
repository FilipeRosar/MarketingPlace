using MailKit.Net.Smtp;
using MarketplaceArtesanato.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketplaceArtesanato.Services.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendApprovalEmailAsync(string toEmail, string sellerName)
        {
            var email = new MimeMessage();
            email.From.Add(MailboxAddress.Parse(_configuration["Email:From"]));
            email.To.Add(MailboxAddress.Parse(toEmail));
            email.Subject = "🎉 Sua loja foi aprovada na Trama Artesanato!";

            var builder = new BodyBuilder();
            builder.HtmlBody = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: auto; padding: 20px; border: 4px solid #fb923c; border-radius: 20px; background: linear-gradient(to bottom, #fff7ed, #fff);'>
                    <div style='text-align: center; padding: 20px;'>
                        <h1 style='color: #ea580c; font-size: 36px;'>🎨 Bem-vindo à Trama!</h1>
                    </div>
                    <div style='padding: 20px; background: white; border-radius: 15px; box-shadow: 0 10px 30px rgba(0,0,0,0.1);'>
                        <p style='font-size: 18px; color: #1f2937;'>Olá <strong>{sellerName}</strong>,</p>
                        <p style='font-size: 18px; color: #1f2937;'>
                            Temos uma ótima notícia: <span style='color: #ea580c; font-weight: bold;'>sua loja foi aprovada!</span>
                        </p>
                        <p style='font-size: 16px; color: #4b5563; line-height: 1.6;'>
                            Agora você já pode acessar sua loja, cadastrar produtos e começar a vender suas artes para todo o Brasil.<br>
                            Sua criatividade tem casa na Trama!
                        </p>
                        <div style='text-align: center; margin: 30px 0;'>
                            <a href='https://sua-plataforma.com/seller/dashboard' 
                               style='background: linear-gradient(to right, #fb923c, #ea580c); color: white; padding: 16px 40px; border-radius: 50px; text-decoration: none; font-weight: bold; font-size: 18px; box-shadow: 0 10px 20px rgba(251, 146, 60, 0.4);'>
                                Acessar Minha Loja
                            </a>
                        </div>
                        <p style='font-size: 14px; color: #6b7280; text-align: center;'>
                            Qualquer dúvida, é só chamar no WhatsApp ou e-mail.<br>
                            Estamos aqui pra te ajudar a crescer! 🚀
                        </p>
                    </div>
                    <div style='text-align: center; margin-top: 30px; color: #9ca3af; font-size: 12px;'>
                        © 2025 Trama Artesanato. Feito com carinho para artesãos como você.
                    </div>
                </div>";

            email.Body = builder.ToMessageBody();

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(_configuration["Email:SmtpHost"], int.Parse(_configuration["Email:SmtpPort"]), MailKit.Security.SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(_configuration["Email:SmtpUser"], _configuration["Email:SmtpPass"]);
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }
    }
}
