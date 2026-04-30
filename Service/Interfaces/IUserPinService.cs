using BO.DTO.Users;

namespace Service.Interfaces;

public interface IUserPinService
{
    Task<PinStatusDto> GetStatusAsync(int userId);
    Task SetPinAsync(int userId, string pin);
    Task<VerifyPinResponseDto> VerifyPinAsync(int userId, string pin);
    Task ChangePinAsync(int userId, string currentPin, string newPin);
    Task RemovePinAsync(int userId, string pin);
}
