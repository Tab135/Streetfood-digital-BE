using BO.DTO.Auth;
using System.Threading.Tasks;

namespace Service.Interfaces
{
    public interface IFacebookService
    {
        Task<FacebookUserInfo> ValidateTokenAndGetUserAsync(string accessToken);
        Task<LoginResponse> FacebookLoginAsync(FacebookAuthDto facebookAuthDto);
    }
}
