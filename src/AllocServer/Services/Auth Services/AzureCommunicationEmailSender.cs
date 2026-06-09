using AllocServer.Configurations;
using AllocServer.Interfaces.Auth;
using Azure;
using Azure.Communication.Email;
using Microsoft.Extensions.Options;

namespace AllocServer.Services.Auth_Services
{
    public class AzureCommunicationEmailSender : IEmailSender
    {
        private readonly EmailSettings _settings;

        public AzureCommunicationEmailSender(IOptions<EmailSettings> options)
        {
            _settings = options.Value;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string bodyHtml)
        {
            var config = _settings.AzureCommunication;

            if (string.IsNullOrEmpty(config.ConnectionString) || string.IsNullOrEmpty(config.SenderEmail))
            {
                throw new InvalidOperationException("Chưa cấu hình thông tin Azure Communication Services Email.");
            }

            var emailClient = new EmailClient(config.ConnectionString);

            await emailClient.SendAsync(
                WaitUntil.Completed,
                senderAddress: config.SenderEmail,
                recipientAddress: toEmail,
                subject: subject,
                htmlContent: bodyHtml);
        }
    }
}
