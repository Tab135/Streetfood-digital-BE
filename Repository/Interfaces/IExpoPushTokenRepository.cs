namespace Repository.Interfaces;

public interface IExpoPushTokenRepository
{
    Task UpsertTokenAsync(int userId, string token, string platform);
    Task<List<string>> GetTokensByUserIdAsync(int userId);
    Task RemoveTokenAsync(string token);
}
