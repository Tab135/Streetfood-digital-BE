using DAL;
using Repository.Interfaces;

namespace Repository;

public class ExpoPushTokenRepository : IExpoPushTokenRepository
{
    private readonly ExpoPushTokenDAO _expoPushTokenDAO;

    public ExpoPushTokenRepository(ExpoPushTokenDAO expoPushTokenDAO)
    {
        _expoPushTokenDAO = expoPushTokenDAO ?? throw new ArgumentNullException(nameof(expoPushTokenDAO));
    }

    public async Task UpsertTokenAsync(int userId, string token, string platform)
    {
        await _expoPushTokenDAO.UpsertTokenAsync(userId, token, platform);
    }

    public async Task<List<string>> GetTokensByUserIdAsync(int userId)
    {
        return await _expoPushTokenDAO.GetTokensByUserIdAsync(userId);
    }

    public async Task RemoveTokenAsync(string token)
    {
        await _expoPushTokenDAO.RemoveTokenAsync(token);
    }
}
