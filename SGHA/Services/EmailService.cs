using MailKit.Net.Smtp;
using Microsoft.Extensions.Configuration;
using MimeKit;
using SGHA.DTO;
using SGHA.Interfaces;

namespace SGHA.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmailAsync(EmailRequestDto emailRequest)
        {
            if (emailRequest == null ||
                string.IsNullOrWhiteSpace(emailRequest.To) ||
                string.IsNullOrWhiteSpace(emailRequest.Subject) ||
                string.IsNullOrWhiteSpace(emailRequest.Body))
            {
                throw new ArgumentException("Invalid email request data.");
            }

            var smtpSettings = _config.GetSection("Smtp");
            var host = smtpSettings["Host"];
            var port = int.Parse(smtpSettings["Port"]);
            var username = smtpSettings["Username"];
            var password = smtpSettings["Password"];
            var fromAddress = smtpSettings["FromAddress"];
            var displayName = smtpSettings["DisplayName"];

            var emailMessage = new MimeMessage
            {
                Subject = emailRequest.Subject,
                Body = new TextPart("html") { Text = emailRequest.Body }
            };

            emailMessage.From.Add(new MailboxAddress(displayName, fromAddress));
            emailMessage.To.Add(new MailboxAddress("", emailRequest.To));

            using var client = new SmtpClient();
            if (port == 465)
                await client.ConnectAsync(host, port, MailKit.Security.SecureSocketOptions.SslOnConnect);
            else if (port == 587)
                await client.ConnectAsync(host, port, MailKit.Security.SecureSocketOptions.StartTls);
            else
                throw new Exception("Unsupported SMTP port.");

            await client.AuthenticateAsync(username, password);
            await client.SendAsync(emailMessage);
            await client.DisconnectAsync(true);
        }
    }
}
