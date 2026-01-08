using BO.Entities;
using Google.Apis.Auth;
using System.Threading.Tasks;

namespace Repository.Interfaces
{
    public interface IUserRepository
    {
        Task<User> CreateAsync(User user);
        Task<bool> EmailExistsAsync(string email);
        Task<User> GetByEmailAsync(string email);
        Task<User?> GetByUsernameAsync(string username);
        Task UpdateAsync(User user);
        Task<bool> UsernameExistsAsync(string username);
        Task<User> FindOrCreateUserFromGoogleAsync(GoogleJsonWebSignature.Payload payload);
        Task<User> GetUserById(int userId);
        Task UpdatePasswordAsync(int userId, string hashedPassword);
    }
}
