using BO.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DAL
{
    public class OtpVerifyDAO
    {
        private readonly StreetFoodDbContext _context;

        public OtpVerifyDAO(StreetFoodDbContext context)
        {
            _context = context;
        }

        public async Task<int> DeleteExpiredOtpsAsync()
        {
            var now = DateTime.UtcNow;
            var expiredOtps = _context.OtpVerifies
                .Where(o => o.ExpiredAt <= now && o.IsUsed == false);

            _context.OtpVerifies.RemoveRange(expiredOtps);
            return await _context.SaveChangesAsync();
        }

        public async Task CreateAsync(OtpVerify otp)
        {
            _context.OtpVerifies.Add(otp);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAllOtpsByEmailAsync(string email)
        {
            var otps = _context.OtpVerifies.Where(o => o.Email.ToLower() == email.ToLower());
            _context.OtpVerifies.RemoveRange(otps);
            await _context.SaveChangesAsync();
        }

        public async Task<OtpVerify> DeleteUsedOtpAsync(string email, string otpCode)
        {
            var otp = await _context.OtpVerifies
                .FirstOrDefaultAsync(o => o.Email == email && o.Otp == otpCode && o.IsUsed);
            if (otp != null)
            {
                _context.OtpVerifies.Remove(otp);
                await _context.SaveChangesAsync();
            }
            return otp;
        }

        public async Task MarkOtpAsUsedAsync(int otpId)
        {
            var otp = await _context.OtpVerifies.FindAsync(otpId);
            if (otp != null)
            {
                otp.IsUsed = true;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<OtpVerify>> GetRecentOtpsAsync(string email, TimeSpan timeWindow)
        {
            var timeThreshold = DateTime.UtcNow - timeWindow;

            return await _context.OtpVerifies
                .Where(o => o.Email.ToLower() == email.ToLower() &&
                           o.CreatedAt >= timeThreshold)
                .ToListAsync();
        }

        public async Task<(OtpVerify? otp, string? error)> GetValidOtpWithDetailAsync(string email, string otp)
        {
            var currentTime = DateTime.UtcNow;

            var otpRecord = await _context.OtpVerifies
                .Where(o => o.Email.ToLower() == email.ToLower() &&
                           o.Otp == otp)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();

            if (otpRecord == null)
            {
                return (null, "Invalid OTP code");
            }

            if (otpRecord.IsUsed)
            {
                return (null, "OTP has already been used");
            }

            if (otpRecord.ExpiredAt <= currentTime)
            {
                _context.OtpVerifies.Remove(otpRecord);
                await _context.SaveChangesAsync();
                return (null, "OTP has expired");
            }

            return (otpRecord, null); // No error
        }
    }
}