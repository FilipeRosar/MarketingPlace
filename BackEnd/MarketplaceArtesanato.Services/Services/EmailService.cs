using MailKit.Net.Smtp;
using MarketplaceArtesanato.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using MimeKit;
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

            var fromName = _configuration["Email:FromName"];
            var fromEmail = _configuration["Email:FromEmail"];

            email.From.Add(new MailboxAddress(fromName, fromEmail));
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
                            <a href='http://localhost:4200/login' 
                               style='background: linear-gradient(to right, #fb923c, #ea580c); color: white; padding: 16px 40px; border-radius: 50px; text-decoration: none; font-weight: bold; font-size: 18px; box-shadow: 0 10px 20px rgba(251, 146, 60, 0.4);'>
                                Acessar Minha Loja
                            </a>
                        </div>
                    </div>
                </div>";

            email.Body = builder.ToMessageBody();

            var smtpServer = _configuration["Email:SmtpServer"];
            var smtpPort = int.Parse(_configuration["Email:SmtpPort"]);
            var smtpUser = _configuration["Email:Username"];
            var smtpPass = _configuration["Email:Password"];

            using var smtp = new SmtpClient();

            await smtp.ConnectAsync(smtpServer, smtpPort, MailKit.Security.SecureSocketOptions.StartTls);

            await smtp.AuthenticateAsync(smtpUser, smtpPass);
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }
        public async Task SendPasswordResetEmailAsync(string toEmail, string userName, string resetLink)
        {
            var email = new MimeMessage();
            var fromName = _configuration["Email:FromName"];
            var fromEmail = _configuration["Email:FromEmail"];

            email.From.Add(new MailboxAddress(fromName, fromEmail));
            email.To.Add(MailboxAddress.Parse(toEmail));
            email.Subject = "🔑 Recuperação de Senha - Trama Artesanato";

            var builder = new BodyBuilder();
            builder.HtmlBody = $@"
        <div style='font-family: Arial, sans-serif; max-width: 600px; margin: auto; padding: 20px; border: 1px solid #e5e7eb; border-radius: 10px;'>
            <h2 style='color: #ea580c;'>Recuperação de Senha</h2>
            <p>Olá <strong>{userName}</strong>,</p>
            <p>Recebemos uma solicitação para redefinir sua senha. Se não foi você, ignore este email.</p>
            <div style='text-align: center; margin: 30px 0;'>
                <a href='{resetLink}' 
                   style='background-color: #ea580c; color: white; padding: 12px 24px; border-radius: 5px; text-decoration: none; font-weight: bold;'>
                    Redefinir Senha
                </a>
            </div>
            <p style='color: #6b7280; font-size: 12px;'>Este link expira em 1 hora.</p>
        </div>";

            email.Body = builder.ToMessageBody();

            var smtpServer = _configuration["Email:SmtpServer"];
            var smtpPort = int.Parse(_configuration["Email:SmtpPort"]);
            var smtpUser = _configuration["Email:Username"];
            var smtpPass = _configuration["Email:Password"];

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(smtpServer, smtpPort, MailKit.Security.SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(smtpUser, smtpPass);
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }
    }
}