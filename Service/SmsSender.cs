using Microsoft.Extensions.Configuration;
using Service.Interfaces;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Service
{
    public class SmsSender : ISmsSender
    {
        private const string BrevoSmsUrl = "https://api.brevo.com/v3/transactionalSMS/sms";

        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;

        public SmsSender(IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
        }

        public async Task SendOtpSmsAsync(string toPhone, string otp, int validMinutes)
        {
            var apiKey = _configuration["Brevo:ApiKey"];
            var sender = _configuration["Brevo:SmsSender"] ?? "StreetFood";

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new Exception("Brevo API Key is missing in configuration.");
            }

            var payload = new
            {
                sender,
                recipient = toPhone,
                content = $"Your OTP code is: {otp}. Valid for {validMinutes} minutes. Do not share this code.",
                type = "transactional",
                charset = "utf-8"
            };

            var json = JsonSerializer.Serialize(payload);
            var request = new HttpRequestMessage(HttpMethod.Post, BrevoSmsUrl)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            request.Headers.Add("api-key", apiKey);
            request.Headers.Add("accept", "application/json");

            var client = _httpClientFactory.CreateClient();
            var response = await client.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to send SMS via Brevo. Status: {(int)response.StatusCode}. Response: {responseBody}");
            }
        }
    }
}
