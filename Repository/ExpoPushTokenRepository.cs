using BO.Entities;
using DAL;
using Microsoft.EntityFrameworkCore;
using Repository.Interfaces;

namespace Repository;

public class ExpoPushTokenRepository(StreetFoodDbContext context) : IExpoPushTokenRepository
{
    public async Task UpsertTokenAsync(int userId, string token, string platform)
    {
        var existing = await context.ExpoPushTokens
            .FirstOrDefaultAsync(t => t.Token == token);

        if (existing != null)
        {
            existing.UserId = userId;
            existing.Platform = platform;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            context.ExpoPushTokens.Add(new ExpoPushToken
            {
                UserId = userId,
                Token = token,
                Platform = platform,
            });
        }

        await context.SaveChangesAsync();
    }

    public async Task<List<string>> GetTokensByUserIdAsync(int userId)
    {
        return await context.ExpoPushTokens
            .Where(t => t.UserId == userId)
            .Select(t => t.Token)
            .ToListAsync();
    }

    public async Task RemoveTokenAsync(string token)
    {
        var entity = await context.ExpoPushTokens
            .FirstOrDefaultAsync(t => t.Token == token);

        if (entity != null)
        {
            context.ExpoPushTokens.Remove(entity);
            await context.SaveChangesAsync();
        }
    }
}
