using BO.DTO.Auth;
using Google.Apis.Auth;
using System.Threading.Tasks;

namespace Service.Interfaces
{
    public interface IGoogleService
    {
        Task<GoogleJsonWebSignature.Payload> ValidateTokenAndGetPayloadAsync(GoogleAuthDto googleAuthDto);
    }
}
