using BO.DTO.Auth;
using Microsoft.Extensions.Configuration;
using Repository.Interfaces;
using Service.Interfaces;
using Service.JWT;
using System;
using System.IdentityModel.Tokens.Jwt; // Ensure you have the System.IdentityModel.Tokens.Jwt NuGet package
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Service
{
    public class FacebookService : IFacebookService
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly IUserRepository _userRepository;
        private readonly IJwtService _jwtService;

        public FacebookService(IConfiguration configuration, IUserRepository userRepository, IJwtService jwtService)
        {
            _configuration = configuration;
            _userRepository = userRepository;
            _jwtService = jwtService;
            // Best practice: In production, use IHttpClientFactory to avoid socket exhaustion
            _httpClient = new HttpClient();
        }

        public async Task<FacebookUserInfo> ValidateTokenAndGetUserAsync(string token)
        {
            if (string.IsNullOrEmpty(token))
                throw new ArgumentNullException(nameof(token));

            // 1. HYBRID CHECK: iOS Limited Login tokens are JWTs starting with "ey"
            if (token.StartsWith("ey"))
            {
                return ValidateOidcToken(token);
            }

            // 2. Fallback to standard Graph API for Android/Web
            return await ValidateGraphTokenAsync(token);
        }

        // New method: perform find-or-create and return LoginResponse (token + user)
        public async Task<LoginResponse> FacebookLoginAsync(FacebookAuthDto facebookAuthDto)
        {
            if (facebookAuthDto == null) throw new ArgumentNullException(nameof(facebookAuthDto));

            var info = await ValidateTokenAndGetUserAsync(facebookAuthDto.AccessToken);

            var email = info.Email ?? $"fb_{info.Id}@facebook.com";

            var user = await _userRepository.FindOrCreateUserFromFacebookAsync(new FacebookUserInfo
            {
                Id = info.Id,
                Email = email,
                Name = info.Name,
                FirstName = info.FirstName,
                LastName = info.LastName,
                AvatarUrl = info.AvatarUrl
            });

            var token = _jwtService.GenerateToken(user);

            return new LoginResponse { Token = token, User = user };
        }

        private FacebookUserInfo ValidateOidcToken(string jwtToken)
        {
            var handler = new JwtSecurityTokenHandler();

            if (!handler.CanReadToken(jwtToken))
                throw new Exception("Invalid JWT format from iOS Limited Login");

            var jwt = handler.ReadJwtToken(jwtToken);

            // Validation
            var fbAppId = _configuration["Facebook:AppId"];
            var audience = jwt.Audiences.FirstOrDefault();
            var issuer = jwt.Issuer;

            // Basic integrity check: Ensure the token was meant for your App
            if (audience != fbAppId || !issuer.Contains("facebook.com"))
                throw new Exception("Facebook Token audience or issuer mismatch");

            // Extract claims
            var claims = jwt.Claims.ToDictionary(c => c.Type, c => c.Value);
            return new FacebookUserInfo
            {
                // In OIDC, 'sub' (Subject) is the Facebook User ID
                Id = claims.GetValueOrDefault("sub") ?? string.Empty,
                Email = claims.GetValueOrDefault("email"),
                Name = claims.GetValueOrDefault("name"),
                FirstName = claims.GetValueOrDefault("given_name"),
                LastName = claims.GetValueOrDefault("family_name"),
                AvatarUrl = claims.GetValueOrDefault("picture")
            };
        }

        private async Task<FacebookUserInfo> ValidateGraphTokenAsync(string accessToken)
        {
            var fbAppId = _configuration["Facebook:AppId"];
            var fbAppSecret = _configuration["Facebook:AppSecret"];

            if (string.IsNullOrEmpty(fbAppId) || string.IsNullOrEmpty(fbAppSecret))
                throw new Exception("Facebook configuration is missing");

            // Validate token validity
            var debugTokenUrl = $"https://graph.facebook.com/debug_token?input_token={accessToken}&access_token={fbAppId}|{fbAppSecret}";
            var debugResp = await _httpClient.GetAsync(debugTokenUrl);

            if (!debugResp.IsSuccessStatusCode)
                throw new Exception("Invalid Facebook token");

            var debugContent = await debugResp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(debugContent);
            var data = doc.RootElement.GetProperty("data");

            if (!data.GetProperty("is_valid").GetBoolean())
                throw new Exception("Invalid Facebook token");

            // Fetch user profile data including picture
            var fields = "id,name,email,first_name,last_name,picture.width(400).height(400){url}";
            var userInfoUrl = $"https://graph.facebook.com/me?fields={fields}&access_token={accessToken}";
            var userInfoResp = await _httpClient.GetAsync(userInfoUrl);

            if (!userInfoResp.IsSuccessStatusCode)
                throw new Exception("Failed to fetch user info from Facebook");

            var userInfoContent = await userInfoResp.Content.ReadAsStringAsync();
            using var userDoc = JsonDocument.Parse(userInfoContent);
            var root = userDoc.RootElement;

            string? AvatarUrl = null;
            if (root.TryGetProperty("picture", out var picProp) && picProp.TryGetProperty("data", out var picData) && picData.TryGetProperty("url", out var urlProp))
            {
                AvatarUrl = urlProp.GetString();
            }

            return new FacebookUserInfo
            {
                Id = root.GetProperty("id").GetString() ?? string.Empty,
                Email = root.TryGetProperty("email", out var emailProp) ? emailProp.GetString() : null,
                Name = root.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : null,
                FirstName = root.TryGetProperty("first_name", out var fnProp) ? fnProp.GetString() : null,
                LastName = root.TryGetProperty("last_name", out var lnProp) ? lnProp.GetString() : null,
                AvatarUrl = AvatarUrl
            };
        }
    }
}