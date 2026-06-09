using System.Net;
using System.Net.Mail;
using AllocServer.Configurations;
using AllocServer.Interfaces.Auth;
using Microsoft.Extensions.Options;

namespace AllocServer.Services.Auth_Services
{
    public class GmailSmtpEmailSender : IEmailSender
    {
        private readonly EmailSettings _settings;

        public GmailSmtpEmailSender(IOptions<EmailSettings> options)
        {
            _settings = options.Value;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string bodyHtml)
        {
            var config = _settings.GmailSmtp;

            if (string.IsNullOrEmpty(config.SenderEmail) || string.IsNullOrEmpty(config.Password))
            {
                throw new InvalidOperationException("Chưa cấu hình tài khoản Gmail gửi mail.");
            }

            using var client = new SmtpClient(config.Host, config.Port)
            {
                Credentials = new NetworkCredential(config.SenderEmail, config.Password),
                EnableSsl = true
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(config.SenderEmail, config.SenderName),
                Subject = subject,
                Body = bodyHtml,
                IsBodyHtml = true
            };
            mailMessage.To.Add(toEmail);

            await client.SendMailAsync(mailMessage);
        }
    }
}
