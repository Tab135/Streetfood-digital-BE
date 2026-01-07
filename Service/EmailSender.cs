using brevo_csharp.Api;
using brevo_csharp.Client;
using brevo_csharp.Model;
using Microsoft.Extensions.Configuration; // Note: This might also cause ambiguity if you aren't careful, but the error here is about Brevo.
using Service.Interfaces;
using System;
using System.Collections.Generic;
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

        public async System.Threading.Tasks.Task SendEmailAsync(string toEmail, string subject, string body)
        {
            // 1. Retrieve Config
            var apiKey = _configuration["Brevo:ApiKey"];
            var senderName = _configuration["Brevo:SenderName"];
            var senderEmail = _configuration["Brevo:SenderEmail"];

            if (string.IsNullOrEmpty(apiKey))
            {
                throw new Exception("Brevo API Key is missing in configuration.");
            }

            // 2. Configure Brevo Client (Global Configuration)
            // FIX: Use the fully qualified name 'brevo_csharp.Client.Configuration'
            if (!brevo_csharp.Client.Configuration.Default.ApiKey.ContainsKey("api-key"))
            {
                brevo_csharp.Client.Configuration.Default.ApiKey.Add("api-key", apiKey);
            }
            else
            {
                brevo_csharp.Client.Configuration.Default.ApiKey["api-key"] = apiKey;
            }

            var apiInstance = new TransactionalEmailsApi();

            // 3. Create Sender & Recipient objects
            var emailSender = new SendSmtpEmailSender(senderName, senderEmail);
            var emailReceiver = new SendSmtpEmailTo(toEmail);
            var recipients = new List<SendSmtpEmailTo> { emailReceiver };

            // 4. Construct the Email
            var sendSmtpEmail = new SendSmtpEmail(
                sender: emailSender,
                to: recipients,
                subject: subject,
                htmlContent: body
            );

            try
            {
                // 5. Send Async
                await apiInstance.SendTransacEmailAsync(sendSmtpEmail);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to send email via Brevo API: {ex.Message}", ex);
            }
        }
    }
}