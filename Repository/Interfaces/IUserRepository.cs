using BO.Entities;
using Google.Apis.Auth;
using BO.DTO.Auth;
using System.Threading.Tasks;

namespace Repository.Interfaces
{
    public interface IUserRepository
    {
        Task<User> CreateAsync(User user);
        Task<bool> EmailExistsAsync(string email);
        Task<User> GetByEmailAsync(string email);
        Task<User?> GetByPhoneNumberAsync(string phoneNumber);
        Task<User?> GetByUsernameAsync(string username);
        Task UpdateAsync(User user);
        Task<bool> UsernameExistsAsync(string username);
        Task<User> FindOrCreateUserFromGoogleAsync(GoogleJsonWebSignature.Payload payload);
        Task<User> FindOrCreateUserFromFacebookAsync(FacebookUserInfo info);
        Task<User> GetUserById(int userId);
        Task UpdatePasswordAsync(int userId, string hashedPassword);
        Task<(System.Collections.Generic.List<User> Users, int TotalCount)> SearchUsersAsync(string keyword, int pageNumber, int pageSize);
    }
}
