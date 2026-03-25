namespace Service.Interfaces;

public interface IExpoPushService
{
    Task RegisterTokenAsync(int userId, string token, string platform);
    Task RemoveTokenAsync(string token);
    Task SendPushToUserAsync(int userId, string title, string body, object? data = null);
}
