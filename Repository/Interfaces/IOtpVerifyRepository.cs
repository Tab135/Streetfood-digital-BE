using BO.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Repository.Interfaces
{
    public interface IOtpVerifyRepository
    {
        Task<int> DeleteExpiredOtpsAsync();
        Task CreateAsync(OtpVerify otp);
        Task DeleteAllOtpsByEmailAsync(string email);
        Task<OtpVerify> DeleteUsedOtpAsync(string email, string otpCode);
        Task MarkOtpAsUsedAsync(int otpId);
        Task<List<OtpVerify>> GetRecentOtpsAsync(string email, TimeSpan timeWindow);
        Task<(OtpVerify? otp, string? error)> GetValidOtpWithDetailAsync(string email, string otp);
    }
}
