using BO;
using BO.Entities;
using Google.Apis.Auth;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using BO.DTO.Auth;

namespace DAL
{
    public class UserDAO
    {
        private readonly StreetFoodDbContext _context;
        public UserDAO(StreetFoodDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }
        public async Task<User> CreateAsync(User user)
        {
            user.Id = 0;
            _context.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            return await _context.Users
                 .AnyAsync(u => u.Email == email);
        }

        public async Task<User> GetByEmailAsync(string email)
        {
            return await _context.Users
                 .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User?> GetByUsernameAsync(string username)
        {
            return await _context.Users
                  .FirstOrDefaultAsync(u => u.UserName == username);
        }

        public async Task<User?> GetByPhoneNumberAsync(string phoneNumber)
        {
            return await _context.Users
                  .FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);
        }

        public async Task UpdateAsync(User user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> UsernameExistsAsync(string username)
        {
            return await _context.Users
                 .AnyAsync(u => u.UserName == username);
        }

        public async Task<User> FindOrCreateUserFromGoogleAsync(GoogleJsonWebSignature.Payload payload)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == payload.Email);

            if (user == null)
            {
                user = new User
                {
                    UserName = payload.Name,
                    Email = payload.Email,
                    Password = "", // No password for Google users
                    Role = Role.User,
                    CreatedAt = DateTime.UtcNow,
                    EmailVerified = true, // Google auth implies verified email
                    FirstName = payload.GivenName,
                    LastName = payload.FamilyName,
                    AvatarUrl = payload.Picture
                };

                await CreateAsync(user);
                user = await GetByEmailAsync(payload.Email); // Reload with Role if needed
            }
            else
            {
                // If user exists but avatar is missing and we have one from Google, update it
                if (string.IsNullOrEmpty(user.AvatarUrl) && !string.IsNullOrEmpty(payload.Picture))
                {
                    user.AvatarUrl = payload.Picture;
                    _context.Users.Update(user);
                    await _context.SaveChangesAsync();
                }
            }

            return user;
        }

        public async Task<User> FindOrCreateUserFromFacebookAsync(FacebookUserInfo info)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == info.Email);

            if (user == null)
            {
                user = new User
                {
                    UserName = info.Name ?? info.Email,
                    Email = info.Email,
                    Password = "",
                    Role = Role.User,
                    CreatedAt = DateTime.UtcNow,
                    EmailVerified = true,
                    FirstName = info.FirstName ?? string.Empty,
                    LastName = info.LastName ?? string.Empty,
                    AvatarUrl = info.AvatarUrl
                };

                await CreateAsync(user);
                user = await GetByEmailAsync(info.Email);
            }
            else
            {
                // If user exists but avatar is missing and we have one from Facebook, update it
                if (string.IsNullOrEmpty(user.AvatarUrl) && !string.IsNullOrEmpty(info.AvatarUrl))
                {
                    user.AvatarUrl = info.AvatarUrl;
                    _context.Users.Update(user);
                    await _context.SaveChangesAsync();
                }
            }

            return user;
        }

        public async Task<User> GetUserById(int userId)
        {
            return await _context.Users.FirstOrDefaultAsync(x => x.Id == userId);
        }

        // Added: update only password
        public async Task UpdatePasswordAsync(int userId, string hashedPassword)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) throw new Exception("User not found");
            user.Password = hashedPassword;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }
    }
}