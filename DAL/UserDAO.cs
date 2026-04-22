using BO;
using BO.Entities;
using Google.Apis.Auth;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
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
                var firstName = (payload.GivenName ?? string.Empty).Trim();
                var lastName = (payload.FamilyName ?? string.Empty).Trim();

                if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
                {
                    var fullName = (payload.Name ?? string.Empty).Trim();
                    if (!string.IsNullOrWhiteSpace(fullName))
                    {
                        var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length == 1)
                        {
                            firstName = string.IsNullOrWhiteSpace(firstName) ? parts[0] : firstName;
                            lastName = string.IsNullOrWhiteSpace(lastName) ? "User" : lastName;
                        }
                        else
                        {
                            if (string.IsNullOrWhiteSpace(firstName))
                            {
                                firstName = parts[0];
                            }

                            if (string.IsNullOrWhiteSpace(lastName))
                            {
                                lastName = string.Join(" ", parts.Skip(1));
                            }
                        }
                    }
                }

                if (string.IsNullOrWhiteSpace(firstName))
                {
                    firstName = "Google";
                }

                if (string.IsNullOrWhiteSpace(lastName))
                {
                    lastName = "User";
                }

                user = new User
                {
                    UserName = payload.Name ?? payload.Email,
                    Email = payload.Email,
                    Role = Role.User,
                    CreatedAt = DateTime.UtcNow,
                    EmailVerified = true, // Google auth implies verified email
                    PhoneNumberVerified = false,
                    FirstName = firstName,
                    LastName = lastName,
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
                    Role = Role.User,
                    CreatedAt = DateTime.UtcNow,
                    EmailVerified = true,
                    PhoneNumberVerified = false,
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
            return await _context.Users
                .Include(u => u.Tier)
                .FirstOrDefaultAsync(x => x.Id == userId);
        }

        public async Task<(System.Collections.Generic.List<User> Users, int TotalCount)> GetUsersAsync(Role? role, int pageNumber, int pageSize)
        {
            if (pageNumber <= 0)
            {
                pageNumber = 1;
            }

            if (pageSize <= 0)
            {
                pageSize = 10;
            }

            var query = _context.Users.AsQueryable();

            if (role.HasValue)
            {
                query = query.Where(u => u.Role == role.Value);
            }

            var totalCount = await query.CountAsync();
            var users = await query
                .OrderBy(u => u.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (users, totalCount);
        }

        public async Task<(System.Collections.Generic.List<User> Users, int TotalCount)> SearchUsersAsync(string keyword, bool onlyUserRole, int pageNumber, int pageSize)
        {
            var query = _context.Users.AsQueryable();

            if (onlyUserRole)
            {
                query = query.Where(u => u.Role == Role.User);
            }

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var lowerKeyword = keyword.ToLower();
                query = query.Where(u => 
                    (u.Email != null && u.Email.ToLower().Contains(lowerKeyword)) ||
                    (u.UserName != null && u.UserName.ToLower().Contains(lowerKeyword)) ||
                    (u.FirstName != null && u.FirstName.ToLower().Contains(lowerKeyword)) ||
                    (u.LastName != null && u.LastName.ToLower().Contains(lowerKeyword)) ||
                    (u.PhoneNumber != null && u.PhoneNumber.Contains(keyword))
                );
            }

            var totalCount = await query.CountAsync();
            var users = await query
                .OrderBy(u => u.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (users, totalCount);
        }

        public async Task<System.Collections.Generic.List<int>> ResetAllCustomerTiersAsync(int goldXP, int diamondXP, System.Threading.CancellationToken ct)
        {
            var customers = await _context.Users.Where(u => (int)u.Role == 0).ToListAsync(ct); // Role.User = 0
            var usersResetToGold = new System.Collections.Generic.List<int>();

            foreach(var c in customers)
            {
                if (c.TierId == 4) // Diamond -> Gold
                {
                    c.TierId = 3;
                    c.XP = goldXP; // Set current XP to the base of Gold
                    usersResetToGold.Add(c.Id);
                }
                else if (c.TierId == 3) // Gold -> Silver
                {
                    c.TierId = 2;
                    c.XP = 0;
                }
                else // Silver and others
                {
                    c.TierId = 2;
                    c.XP = 0;
                }
            }
            await _context.SaveChangesAsync(ct);
            return usersResetToGold;
        }
    }
}