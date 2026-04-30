using BO.DTO.Users;
using DAL;
using Microsoft.EntityFrameworkCore;
using Service.Interfaces;

namespace Service;

public class UserPinService : IUserPinService
{
    private const int MaxAttempts = 5;
    private const int CooldownSeconds = 30;

    private readonly StreetFoodDbContext _context;

    public UserPinService(StreetFoodDbContext context)
    {
        _context = context;
    }

    public async Task<PinStatusDto> GetStatusAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId)
            ?? throw new KeyNotFoundException("User not found");
        return new PinStatusDto { HasPin = user.PinHash != null };
    }

    public async Task SetPinAsync(int userId, string pin)
    {
        var user = await _context.Users.FindAsync(userId)
            ?? throw new KeyNotFoundException("User not found");

        if (user.PinHash != null)
            throw new InvalidOperationException("PIN already set. Use change endpoint.");

        user.PinHash = BCrypt.Net.BCrypt.HashPassword(pin);
        user.PinSetAt = DateTime.UtcNow;
        user.PinAttempts = 0;
        user.PinLockedUntil = null;
        await _context.SaveChangesAsync();
    }

    public async Task<VerifyPinResponseDto> VerifyPinAsync(int userId, string pin)
    {
        var user = await _context.Users.FindAsync(userId)
            ?? throw new KeyNotFoundException("User not found");

        if (user.PinHash == null)
            throw new InvalidOperationException("No PIN set for this account.");

        if (user.PinLockedUntil.HasValue && user.PinLockedUntil.Value > DateTime.UtcNow)
        {
            var retryAfter = (int)Math.Ceiling((user.PinLockedUntil.Value - DateTime.UtcNow).TotalSeconds);
            throw new PinLockedException(retryAfter);
        }

        var correct = BCrypt.Net.BCrypt.Verify(pin, user.PinHash);
        if (correct)
        {
            user.PinAttempts = 0;
            user.PinLockedUntil = null;
            await _context.SaveChangesAsync();
            return new VerifyPinResponseDto { Success = true };
        }

        user.PinAttempts++;
        if (user.PinAttempts >= MaxAttempts)
        {
            user.PinLockedUntil = DateTime.UtcNow.AddSeconds(CooldownSeconds);
            user.PinAttempts = 0;
        }
        await _context.SaveChangesAsync();

        var remaining = user.PinLockedUntil.HasValue ? 0 : MaxAttempts - user.PinAttempts;
        return new VerifyPinResponseDto { Success = false, AttemptsRemaining = remaining };
    }

    public async Task ChangePinAsync(int userId, string currentPin, string newPin)
    {
        var verifyResult = await VerifyPinAsync(userId, currentPin);
        if (!verifyResult.Success)
            throw new UnauthorizedAccessException("Incorrect current PIN.");

        var user = await _context.Users.FindAsync(userId)!;
        user!.PinHash = BCrypt.Net.BCrypt.HashPassword(newPin);
        user.PinSetAt = DateTime.UtcNow;
        user.PinAttempts = 0;
        user.PinLockedUntil = null;
        await _context.SaveChangesAsync();
    }

    public async Task RemovePinAsync(int userId, string pin)
    {
        var verifyResult = await VerifyPinAsync(userId, pin);
        if (!verifyResult.Success)
            throw new UnauthorizedAccessException("Incorrect PIN.");

        var user = await _context.Users.FindAsync(userId)!;
        user!.PinHash = null;
        user.PinSetAt = null;
        user.PinAttempts = 0;
        user.PinLockedUntil = null;
        await _context.SaveChangesAsync();
    }
}

public class PinLockedException : Exception
{
    public int RetryAfterSeconds { get; }

    public PinLockedException(int retryAfterSeconds)
        : base($"Too many failed attempts. Try again in {retryAfterSeconds} seconds.")
    {
        RetryAfterSeconds = retryAfterSeconds;
    }
}
