using System.Net.Http.Json;
using Repository.Interfaces;
using Service.Interfaces;

namespace Service;

public class ExpoPushService(
    IExpoPushTokenRepository tokenRepository,
    IHttpClientFactory httpClientFactory
) : IExpoPushService
{
    private const string ExpoPushUrl = "https://exp.host/--/api/v2/push/send";

    public async Task RegisterTokenAsync(int userId, string token, string platform)
    {
        await tokenRepository.UpsertTokenAsync(userId, token, platform);
    }

    public async Task RemoveTokenAsync(string token)
    {
        await tokenRepository.RemoveTokenAsync(token);
    }

    public async Task SendPushToUserAsync(
        int userId,
        string title,
        string body,
        object? data = null
    )
    {
        var tokens = await tokenRepository.GetTokensByUserIdAsync(userId);
        if (tokens.Count == 0) return;

        var messages = tokens.Select(token => new
        {
            to = token,
            title,
            body,
            data,
            sound = "default",
            priority = "high",
        });

        var client = httpClientFactory.CreateClient();
        var response = await client.PostAsJsonAsync(ExpoPushUrl, messages);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            Console.Error.WriteLine($"Expo push failed: {response.StatusCode} — {errorBody}");
        }
    }
}
