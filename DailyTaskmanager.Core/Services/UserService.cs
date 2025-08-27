using System;
using System.Data.Entity;
using System.Threading.Tasks;
using DailyTaskManager.Core.Models;
using BCrypt.Net;

namespace DailyTaskManager.Core.Services
{
    public class UserService : IUserService
    {
        public async Task<User> GetUserByIdAsync(int userId)
        {
            using (var context = new DailyTaskManagerDbContext())
            {
                return await context.Users.FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);
            }
        }

        public async Task<User> GetUserByUsernameAsync(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return null;

            using (var context = new DailyTaskManagerDbContext())
            {
                return await context.Users.FirstOrDefaultAsync(u => 
                    u.Username.ToLower() == username.ToLower() && u.IsActive);
            }
        }

        public async Task<User> GetUserByEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return null;

            using (var context = new DailyTaskManagerDbContext())
            {
                return await context.Users.FirstOrDefaultAsync(u => 
                    u.Email.ToLower() == email.ToLower() && u.IsActive);
            }
        }

        public async Task<User> GetUserByOAuthAsync(string provider, string oauthId)
        {
            if (string.IsNullOrWhiteSpace(provider) || string.IsNullOrWhiteSpace(oauthId))
                return null;

            using (var context = new DailyTaskManagerDbContext())
            {
                return await context.Users.FirstOrDefaultAsync(u => 
                    u.OAuthProvider == provider && u.OAuthId == oauthId && u.IsActive);
            }
        }

        public async Task<User> CreateUserAsync(User user)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            user.CreatedAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;

            using (var context = new DailyTaskManagerDbContext())
            {
                context.Users.Add(user);
                await context.SaveChangesAsync();
                return user;
            }
        }

        public async Task<User> UpdateUserAsync(User user)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            using (var context = new DailyTaskManagerDbContext())
            {
                user.UpdatedAt = DateTime.UtcNow;
                context.Entry(user).State = EntityState.Modified;
                await context.SaveChangesAsync();
                return user;
            }
        }

        public async Task<bool> DeleteUserAsync(int userId)
        {
            using (var context = new DailyTaskManagerDbContext())
            {
                var user = await context.Users.FindAsync(userId);
                if (user == null)
                    return false;

                // Soft delete
                user.IsActive = false;
                user.UpdatedAt = DateTime.UtcNow;
                await context.SaveChangesAsync();
                return true;
            }
        }

        public async Task<bool> ValidatePasswordAsync(User user, string password)
        {
            if (user == null || string.IsNullOrWhiteSpace(password))
                return false;

            if (string.IsNullOrWhiteSpace(user.PasswordHash))
                return false;

            return await Task.Run(() => BCrypt.Net.BCrypt.Verify(password, user.PasswordHash));
        }

        public async Task<User> CreateOAuthUserAsync(string email, string displayName, string provider, string oauthId)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(provider) || string.IsNullOrWhiteSpace(oauthId))
                throw new ArgumentException("Email, provider, and OAuth ID are required");

            var user = new User
            {
                Email = email,
                DisplayName = displayName ?? email,
                Username = GenerateUsernameFromEmail(email),
                OAuthProvider = provider,
                OAuthId = oauthId,
                IsActive = true
            };

            return await CreateUserAsync(user);
        }

        private string GenerateUsernameFromEmail(string email)
        {
            var atIndex = email.IndexOf('@');
            if (atIndex > 0)
            {
                return email.Substring(0, atIndex);
            }
            return email;
        }

        public static string HashPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Password cannot be null or empty", nameof(password));

            return BCrypt.Net.BCrypt.HashPassword(password);
        }
    }
}