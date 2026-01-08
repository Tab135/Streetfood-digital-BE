using BO.Entities;
using DAL;
using Google.Apis.Auth;
using Repository.Interfaces;
using System;
using System.Threading.Tasks;

namespace Repository
{
    public class UserRepository : IUserRepository
    {
        private readonly UserDAO _userDAO;

        public UserRepository(UserDAO userDAO)
        {
            _userDAO = userDAO ?? throw new ArgumentNullException(nameof(userDAO));
        }

        public async Task<User> CreateAsync(User user)
        {
            return await _userDAO.CreateAsync(user);
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            return await _userDAO.EmailExistsAsync(email);
        }

        public async Task<User> GetByEmailAsync(string email)
        {
            return await _userDAO.GetByEmailAsync(email);
        }

        public async Task<User?> GetByUsernameAsync(string username)
        {
            return await _userDAO.GetByUsernameAsync(username);
        }

        public async Task UpdateAsync(User user)
        {
            await _userDAO.UpdateAsync(user);
        }

        public async Task<bool> UsernameExistsAsync(string username)
        {
            return await _userDAO.UsernameExistsAsync(username);
        }

        public async Task<User> FindOrCreateUserFromGoogleAsync(GoogleJsonWebSignature.Payload payload)
        {
            return await _userDAO.FindOrCreateUserFromGoogleAsync(payload);
        }

        public async Task<User> GetUserById(int userId)
        {
            return await _userDAO.GetUserById(userId);
        }

        public async Task UpdatePasswordAsync(int userId, string hashedPassword)
        {
            await _userDAO.UpdatePasswordAsync(userId, hashedPassword);
        }
    }
}
