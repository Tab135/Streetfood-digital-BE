using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace Service
{
    public class EmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;

        public EmailSender(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                var smtpHost = _configuration["SmtpSettings:Host"];
                var smtpPort = int.Parse(_configuration["SmtpSettings:Port"]);
                var smtpUsername = _configuration["SmtpSettings:Username"];
                var smtpPassword = _configuration["SmtpSettings:Password"];
                var senderEmail = _configuration["SmtpSettings:SenderEmail"];
                var senderName = _configuration["SmtpSettings:SenderName"];

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(senderName, senderEmail));
                message.To.Add(new MailboxAddress("", toEmail));
                message.Subject = subject;
                message.Body = new TextPart("html") { Text = body };

                using (var client = new SmtpClient())
                {
                    await client.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.StartTls);
                    await client.AuthenticateAsync(smtpUsername, smtpPassword);
                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to send email: {ex.Message}");
            }
        }
    }
}
