using BO.Entities;
using Microsoft.EntityFrameworkCore;

namespace DAL;

public class UserPinDAO
{
    private readonly StreetFoodDbContext _context;

    public UserPinDAO(StreetFoodDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<User?> GetByIdAsync(int userId)
    {
        return await _context.Users.FindAsync(userId);
    }

    public async Task UpdateAsync(User user)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
    }
}