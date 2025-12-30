using BO.Entities;
using DAL;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Repository
{
    public class OtpVerifyRepository : IOtpVerifyRepository
    {
        private readonly OtpVerifyDAO _otpVerifyDAO;

        public OtpVerifyRepository(OtpVerifyDAO otpVerifyDAO)
        {
            _otpVerifyDAO = otpVerifyDAO;
        }

        public async Task<int> DeleteExpiredOtpsAsync()
        {
            return await _otpVerifyDAO.DeleteExpiredOtpsAsync();
        }

        public async Task CreateAsync(OtpVerify otp)
        {
            await _otpVerifyDAO.CreateAsync(otp);
        }

        public async Task DeleteAllOtpsByEmailAsync(string email)
        {
            await _otpVerifyDAO.DeleteAllOtpsByEmailAsync(email);
        }

        public async Task<OtpVerify> DeleteUsedOtpAsync(string email, string otpCode)
        {
            return await _otpVerifyDAO.DeleteUsedOtpAsync(email, otpCode);
        }

        public async Task MarkOtpAsUsedAsync(int otpId)
        {
            await _otpVerifyDAO.MarkOtpAsUsedAsync(otpId);
        }

        public async Task<List<OtpVerify>> GetRecentOtpsAsync(string email, TimeSpan timeWindow)
        {
            return await _otpVerifyDAO.GetRecentOtpsAsync(email, timeWindow);
        }

        public async Task<(OtpVerify? otp, string? error)> GetValidOtpWithDetailAsync(string email, string otp)
        {
            return await _otpVerifyDAO.GetValidOtpWithDetailAsync(email, otp);
        }
    }
}
