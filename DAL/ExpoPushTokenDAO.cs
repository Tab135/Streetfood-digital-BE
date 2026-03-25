using BO.Entities;
using Microsoft.EntityFrameworkCore;

namespace DAL;

public class ExpoPushTokenDAO
{
    private readonly StreetFoodDbContext _context;

    public ExpoPushTokenDAO(StreetFoodDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task UpsertTokenAsync(int userId, string token, string platform)
    {
        var existing = await _context.ExpoPushTokens
            .FirstOrDefaultAsync(t => t.Token == token);

        if (existing != null)
        {
            existing.UserId = userId;
            existing.Platform = platform;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            _context.ExpoPushTokens.Add(new ExpoPushToken
            {
                UserId = userId,
                Token = token,
                Platform = platform,
            });
        }

        await _context.SaveChangesAsync();
    }

    public async Task<List<string>> GetTokensByUserIdAsync(int userId)
    {
        return await _context.ExpoPushTokens
            .Where(t => t.UserId == userId)
            .Select(t => t.Token)
            .ToListAsync();
    }

    public async Task RemoveTokenAsync(string token)
    {
        var entity = await _context.ExpoPushTokens
            .FirstOrDefaultAsync(t => t.Token == token);

        if (entity != null)
        {
            _context.ExpoPushTokens.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
}