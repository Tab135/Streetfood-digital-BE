using BO.DTO.Auth;
using BO.Exceptions;
using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Service.Interfaces;

namespace Service
{
    public class GoogleService : IGoogleService
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public GoogleService(IConfiguration configuration)
        {
            _configuration = configuration;
            _httpClient = new HttpClient();
        }

        public async Task<GoogleJsonWebSignature.Payload> ValidateTokenAndGetPayloadAsync(GoogleAuthDto googleAuthDto)
        {
            GoogleJsonWebSignature.Payload payload;

            // Handle AccessToken flow (for web with useGoogleLogin)
            if (!string.IsNullOrEmpty(googleAuthDto.AccessToken))
            {
                var response = await _httpClient.GetAsync($"https://www.googleapis.com/oauth2/v3/userinfo?access_token={googleAuthDto.AccessToken}");
                
                if (!response.IsSuccessStatusCode)
                {
                    throw new DomainExceptions("Token Google không hợp lệ");
                }

                var userInfoJson = await response.Content.ReadAsStringAsync();
                var userInfo = JsonSerializer.Deserialize<GoogleUserInfo>(userInfoJson);

                if (userInfo == null || string.IsNullOrEmpty(userInfo.Sub))
                {
                    throw new DomainExceptions("Không lấy được thông tin người dùng từ Google");
                }

                // Convert Google user info to payload format
                payload = new GoogleJsonWebSignature.Payload
                {
                    Subject = userInfo.Sub,
                    Email = userInfo.Email,
                    Name = userInfo.Name,
                    GivenName = userInfo.GivenName,
                    FamilyName = userInfo.FamilyName,
                    Picture = userInfo.Picture,
                    EmailVerified = userInfo.EmailVerified
                };
            }
            // Handle IdToken flow (for mobile/existing implementation)
            else if (!string.IsNullOrEmpty(googleAuthDto.IdToken))
            {
                payload = await GoogleJsonWebSignature.ValidateAsync(
                    googleAuthDto.IdToken,
                    new GoogleJsonWebSignature.ValidationSettings
                    {
                        Audience = new[] { _configuration["GoogleAuth:ClientId"] }
                    });
            }
            else
            {
                throw new DomainExceptions("Cần cung cấp IdToken hoặc AccessToken");
            }

            return payload;
        }
    }
}
