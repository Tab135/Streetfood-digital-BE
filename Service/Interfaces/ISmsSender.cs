using System.Threading.Tasks;

namespace Service.Interfaces
{
    public interface ISmsSender
    {
        Task SendOtpSmsAsync(string toPhone, string otp, int validMinutes);
    }
}
