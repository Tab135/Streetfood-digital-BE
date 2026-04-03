using BO.Entities;
using DAL;
using Google.Apis.Auth;
using BO.DTO.Auth;
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

        public async Task<User?> GetByPhoneNumberAsync(string phoneNumber)
        {
            return await _userDAO.GetByPhoneNumberAsync(phoneNumber);
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

        public async Task<User> FindOrCreateUserFromFacebookAsync(FacebookUserInfo info)
        {
            return await _userDAO.FindOrCreateUserFromFacebookAsync(info);
        }

        public async Task<User> GetUserById(int userId)
        {
            return await _userDAO.GetUserById(userId);
        }

        public async Task UpdatePasswordAsync(int userId, string hashedPassword)
        {
            await _userDAO.UpdatePasswordAsync(userId, hashedPassword);
        }

        public async Task<(System.Collections.Generic.List<User> Users, int TotalCount)> GetUsersAsync(Role? role, int pageNumber, int pageSize)
        {
            return await _userDAO.GetUsersAsync(role, pageNumber, pageSize);
        }

        public async Task<(System.Collections.Generic.List<User> Users, int TotalCount)> SearchUsersAsync(string keyword, int pageNumber, int pageSize)
        {
            return await _userDAO.SearchUsersAsync(keyword, pageNumber, pageSize);
        }
    }
}
