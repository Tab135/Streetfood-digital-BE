using System;
using System.Threading.Tasks;
using BO.Entities;
using DAL;
using Microsoft.EntityFrameworkCore;
using Service;
using Xunit;

namespace StreetFood.Tests.Pin
{
    public class UserPinServiceTests : IDisposable
    {
        private readonly StreetFoodDbContext _context;
        private readonly UserPinService _pinService;

        private const string ValidPin = "123456";
        private const string AnotherPin = "654321";

        public UserPinServiceTests()
        {
            var options = new DbContextOptionsBuilder<StreetFoodDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new StreetFoodDbContext(options);
            _pinService = new UserPinService(_context);
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private async Task<User> SeedUserAsync(int id = 1, string? pinHash = null)
        {
            var user = new User
            {
                Id = id,
                FirstName = "Test",
                LastName = "User",
                PinHash = pinHash,
                PinAttempts = 0,
                PinLockedUntil = null,
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

        // ── GetStatusAsync ────────────────────────────────────────────────────

        // SV_PIN_01 (UTCID01) – User with no PIN → hasPin false
        [Fact]
        public async Task GetStatusAsync_UserWithNoPin_ReturnsFalse()
        {
            await SeedUserAsync(id: 1);

            var result = await _pinService.GetStatusAsync(1);

            Assert.False(result.HasPin);
        }

        // SV_PIN_01 (UTCID02) – User with PIN set → hasPin true
        [Fact]
        public async Task GetStatusAsync_UserWithPin_ReturnsTrue()
        {
            await SeedUserAsync(id: 1, pinHash: BCrypt.Net.BCrypt.HashPassword(ValidPin));

            var result = await _pinService.GetStatusAsync(1);

            Assert.True(result.HasPin);
        }

        // SV_PIN_01 (UTCID03) – Non-existent user → KeyNotFoundException
        [Fact]
        public async Task GetStatusAsync_UnknownUser_ThrowsKeyNotFoundException()
        {
            await Assert.ThrowsAsync<KeyNotFoundException>(() => _pinService.GetStatusAsync(999));
        }

        // ── SetPinAsync ───────────────────────────────────────────────────────

        // SV_PIN_02 (UTCID01) – First-time set succeeds, hash stored
        [Fact]
        public async Task SetPinAsync_NoExistingPin_HashesAndSavesPin()
        {
            await SeedUserAsync(id: 1);

            await _pinService.SetPinAsync(1, ValidPin);

            var user = await _context.Users.FindAsync(1);
            Assert.NotNull(user!.PinHash);
            Assert.True(BCrypt.Net.BCrypt.Verify(ValidPin, user.PinHash));
            Assert.NotNull(user.PinSetAt);
            Assert.Equal(0, user.PinAttempts);
            Assert.Null(user.PinLockedUntil);
        }

        // SV_PIN_02 (UTCID02) – PIN already set → InvalidOperationException (409)
        [Fact]
        public async Task SetPinAsync_PinAlreadySet_ThrowsInvalidOperationException()
        {
            await SeedUserAsync(id: 1, pinHash: BCrypt.Net.BCrypt.HashPassword(ValidPin));

            await Assert.ThrowsAsync<InvalidOperationException>(() => _pinService.SetPinAsync(1, AnotherPin));
        }

        // SV_PIN_02 (UTCID03) – Non-existent user → KeyNotFoundException
        [Fact]
        public async Task SetPinAsync_UnknownUser_ThrowsKeyNotFoundException()
        {
            await Assert.ThrowsAsync<KeyNotFoundException>(() => _pinService.SetPinAsync(999, ValidPin));
        }

        // ── VerifyPinAsync ────────────────────────────────────────────────────

        // SV_PIN_03 (UTCID01) – Correct PIN → success, counter reset
        [Fact]
        public async Task VerifyPinAsync_CorrectPin_ReturnsSuccessAndResetsCounter()
        {
            await SeedUserAsync(id: 1, pinHash: BCrypt.Net.BCrypt.HashPassword(ValidPin));

            var result = await _pinService.VerifyPinAsync(1, ValidPin);

            Assert.True(result.Success);
            var user = await _context.Users.FindAsync(1);
            Assert.Equal(0, user!.PinAttempts);
            Assert.Null(user.PinLockedUntil);
        }

        // SV_PIN_03 (UTCID02) – Wrong PIN → failure, attempt counter incremented
        [Fact]
        public async Task VerifyPinAsync_WrongPin_ReturnsFalseAndIncrementsAttempts()
        {
            await SeedUserAsync(id: 1, pinHash: BCrypt.Net.BCrypt.HashPassword(ValidPin));

            var result = await _pinService.VerifyPinAsync(1, "000000");

            Assert.False(result.Success);
            Assert.Equal(4, result.AttemptsRemaining); // 5 - 1
            var user = await _context.Users.FindAsync(1);
            Assert.Equal(1, user!.PinAttempts);
        }

        // SV_PIN_03 (UTCID03) – 5th wrong attempt triggers 30-second cooldown
        [Fact]
        public async Task VerifyPinAsync_FifthWrongAttempt_LocksCooldown()
        {
            var user = await SeedUserAsync(id: 1, pinHash: BCrypt.Net.BCrypt.HashPassword(ValidPin));
            user.PinAttempts = 4; // already at 4 failed attempts
            await _context.SaveChangesAsync();

            var result = await _pinService.VerifyPinAsync(1, "000000");

            Assert.False(result.Success);
            Assert.Equal(0, result.AttemptsRemaining);
            var updated = await _context.Users.FindAsync(1);
            Assert.NotNull(updated!.PinLockedUntil);
            Assert.True(updated.PinLockedUntil.Value > DateTime.UtcNow);
            Assert.Equal(0, updated.PinAttempts);
        }

        // SV_PIN_03 (UTCID04) – Account locked → PinLockedException with retryAfter
        [Fact]
        public async Task VerifyPinAsync_AccountLocked_ThrowsPinLockedException()
        {
            var user = await SeedUserAsync(id: 1, pinHash: BCrypt.Net.BCrypt.HashPassword(ValidPin));
            user.PinLockedUntil = DateTime.UtcNow.AddSeconds(25);
            await _context.SaveChangesAsync();

            var ex = await Assert.ThrowsAsync<PinLockedException>(() => _pinService.VerifyPinAsync(1, ValidPin));
            Assert.True(ex.RetryAfterSeconds > 0);
            Assert.True(ex.RetryAfterSeconds <= 25);
        }

        // SV_PIN_03 (UTCID05) – No PIN set → InvalidOperationException
        [Fact]
        public async Task VerifyPinAsync_NoPinSet_ThrowsInvalidOperationException()
        {
            await SeedUserAsync(id: 1);

            await Assert.ThrowsAsync<InvalidOperationException>(() => _pinService.VerifyPinAsync(1, ValidPin));
        }

        // SV_PIN_03 (UTCID06) – Lock expired → PIN accepted normally
        [Fact]
        public async Task VerifyPinAsync_LockExpired_AllowsVerification()
        {
            var user = await SeedUserAsync(id: 1, pinHash: BCrypt.Net.BCrypt.HashPassword(ValidPin));
            user.PinLockedUntil = DateTime.UtcNow.AddSeconds(-1); // lock has expired
            await _context.SaveChangesAsync();

            var result = await _pinService.VerifyPinAsync(1, ValidPin);

            Assert.True(result.Success);
        }

        // ── ChangePinAsync ────────────────────────────────────────────────────

        // SV_PIN_04 (UTCID01) – Correct current PIN → new PIN saved
        [Fact]
        public async Task ChangePinAsync_CorrectCurrentPin_SavesNewPin()
        {
            await SeedUserAsync(id: 1, pinHash: BCrypt.Net.BCrypt.HashPassword(ValidPin));

            await _pinService.ChangePinAsync(1, ValidPin, AnotherPin);

            var user = await _context.Users.FindAsync(1);
            Assert.NotNull(user!.PinHash);
            Assert.True(BCrypt.Net.BCrypt.Verify(AnotherPin, user.PinHash));
            Assert.False(BCrypt.Net.BCrypt.Verify(ValidPin, user.PinHash));
            Assert.Equal(0, user.PinAttempts);
            Assert.Null(user.PinLockedUntil);
        }

        // SV_PIN_04 (UTCID02) – Wrong current PIN → UnauthorizedAccessException
        [Fact]
        public async Task ChangePinAsync_WrongCurrentPin_ThrowsUnauthorizedAccessException()
        {
            await SeedUserAsync(id: 1, pinHash: BCrypt.Net.BCrypt.HashPassword(ValidPin));

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _pinService.ChangePinAsync(1, "000000", AnotherPin));
        }

        // SV_PIN_04 (UTCID03) – Locked account → PinLockedException propagated
        [Fact]
        public async Task ChangePinAsync_AccountLocked_ThrowsPinLockedException()
        {
            var user = await SeedUserAsync(id: 1, pinHash: BCrypt.Net.BCrypt.HashPassword(ValidPin));
            user.PinLockedUntil = DateTime.UtcNow.AddSeconds(20);
            await _context.SaveChangesAsync();

            await Assert.ThrowsAsync<PinLockedException>(() =>
                _pinService.ChangePinAsync(1, ValidPin, AnotherPin));
        }

        // ── RemovePinAsync ────────────────────────────────────────────────────

        // SV_PIN_05 (UTCID01) – Correct PIN → all PIN fields nulled out
        [Fact]
        public async Task RemovePinAsync_CorrectPin_ClearsPinFields()
        {
            await SeedUserAsync(id: 1, pinHash: BCrypt.Net.BCrypt.HashPassword(ValidPin));

            await _pinService.RemovePinAsync(1, ValidPin);

            var user = await _context.Users.FindAsync(1);
            Assert.Null(user!.PinHash);
            Assert.Null(user.PinSetAt);
            Assert.Equal(0, user.PinAttempts);
            Assert.Null(user.PinLockedUntil);
        }

        // SV_PIN_05 (UTCID02) – Wrong PIN → UnauthorizedAccessException, PIN unchanged
        [Fact]
        public async Task RemovePinAsync_WrongPin_ThrowsUnauthorizedAccessException()
        {
            var hash = BCrypt.Net.BCrypt.HashPassword(ValidPin);
            await SeedUserAsync(id: 1, pinHash: hash);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _pinService.RemovePinAsync(1, "000000"));

            var user = await _context.Users.FindAsync(1);
            Assert.NotNull(user!.PinHash); // unchanged
        }

        // SV_PIN_05 (UTCID03) – Locked account → PinLockedException propagated
        [Fact]
        public async Task RemovePinAsync_AccountLocked_ThrowsPinLockedException()
        {
            var user = await SeedUserAsync(id: 1, pinHash: BCrypt.Net.BCrypt.HashPassword(ValidPin));
            user.PinLockedUntil = DateTime.UtcNow.AddSeconds(20);
            await _context.SaveChangesAsync();

            await Assert.ThrowsAsync<PinLockedException>(() =>
                _pinService.RemovePinAsync(1, ValidPin));
        }
    }
}
