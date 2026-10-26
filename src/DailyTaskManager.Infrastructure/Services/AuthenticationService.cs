using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using BCrypt.Net;
using DailyTaskManager.Core.Interfaces;
using DailyTaskManager.Core.Models;
using DailyTaskManager.Infrastructure.Data;

namespace DailyTaskManager.Infrastructure.Services;

/// <summary>
/// Service for user authentication operations
/// </summary>
public class AuthenticationService : IAuthenticationService
{
    private readonly IDbContextFactory<DailyTaskManagerDbContext> _contextFactory;
    private readonly ILogger<AuthenticationService> _logger;
    private readonly ISessionStore _sessionStore;
    private User? _currentUser;

    private static readonly TimeSpan RememberMeLifetime = TimeSpan.FromDays(30);

    public AuthenticationService(IDbContextFactory<DailyTaskManagerDbContext> contextFactory, ILogger<AuthenticationService> logger, ISessionStore sessionStore)
    {
        _sessionStore = sessionStore;
        _contextFactory = contextFactory;
        _logger = logger;
    }

    public User? CurrentUser => _currentUser;

    public bool IsAuthenticated => _currentUser != null;

    public event EventHandler<AuthResult>? AuthenticationChanged;

    public async Task<AuthResult> LoginAsync(LoginRequest request)
    {
        try
        {
            await using var _context = _contextFactory.CreateDbContext();
            _logger.LogInformation("Login attempt for user: {Username}", request.Username);

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == request.Username && u.IsActive);

            if (user == null)
            {
                _logger.LogWarning("Login failed: User {Username} not found", request.Username);
                return AuthResult.Failure("Invalid username or password");
            }

            if (string.IsNullOrEmpty(user.PasswordHash))
            {
                if (!string.IsNullOrEmpty(user.OAuthProvider))
                {
                    _logger.LogWarning("Login failed: User {Username} is OAuth-only user", request.Username);
                    return AuthResult.Failure("This account uses OAuth login. Please use the OAuth login button.");
                }
                else
                {
                    _logger.LogWarning("Login failed: User {Username} has no password set", request.Username);
                    return AuthResult.Failure("This account has no password set. Please contact your administrator.");
                }
            }

            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                _logger.LogWarning("Login failed: Invalid password for user {Username}", request.Username);
                return AuthResult.Failure("Invalid username or password");
            }

            _currentUser = user;
            var result = AuthResult.Success(user);
            
            AuthenticationChanged?.Invoke(this, result);
            _logger.LogInformation("User {Username} logged in successfully", request.Username);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for user {Username}", request.Username);
            return AuthResult.Failure("An error occurred during login. Please try again.");
        }
    }

    public async Task<AuthResult> LoginWithOAuthAsync(OAuthResult oauthResult)
    {
        try
        {
            await using var _context = _contextFactory.CreateDbContext();
            _logger.LogInformation("OAuth login attempt for user: {Email}", oauthResult.Email);

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == oauthResult.Email && u.IsActive);

            if (user == null)
            {
                // Create new OAuth user with a username that doesn't clash with existing ones
                var baseUsername = oauthResult.Email?.Split('@')[0] ?? "user";
                var username = baseUsername;
                var suffix = 1;
                while (await _context.Users.AnyAsync(u => u.Username == username))
                {
                    username = $"{baseUsername}{++suffix}";
                }

                user = new User
                {
                    Username = username,
                    Email = oauthResult.Email ?? "",
                    DisplayName = oauthResult.DisplayName,
                    OAuthProvider = oauthResult.Provider,
                    OAuthId = oauthResult.ProviderId,
                    IsActive = true
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Created new OAuth user: {Email}", oauthResult.Email);
            }
            else if (!user.IsOAuthUser)
            {
                _logger.LogWarning("OAuth login failed: User {Email} is not OAuth user", oauthResult.Email);
                return AuthResult.Failure("This email is associated with a local account. Please use regular login.");
            }

            _currentUser = user;
            var result = AuthResult.Success(user);
            
            AuthenticationChanged?.Invoke(this, result);
            _logger.LogInformation("User {Email} logged in successfully via OAuth", oauthResult.Email);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during OAuth login for user {Email}", oauthResult.Email);
            return AuthResult.Failure("An error occurred during OAuth login. Please try again.");
        }
    }

    public async Task<AuthResult> RegisterAsync(string username, string email, string password)
    {
        try
        {
            await using var _context = _contextFactory.CreateDbContext();
            username = username?.Trim() ?? string.Empty;
            email = email?.Trim() ?? string.Empty;
            _logger.LogInformation("Registration attempt for user: {Username}, email: {Email}", username, email);

            if (username.Length < 3)
            {
                return AuthResult.Failure("Username must be at least 3 characters");
            }

            if (string.IsNullOrEmpty(password) || password.Length < 8)
            {
                return AuthResult.Failure("Password must be at least 8 characters");
            }

            // Check if user already exists
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username || u.Email == email);

            if (existingUser != null)
            {
                _logger.LogWarning("Registration failed: User {Username} or email {Email} already exists", username, email);
                return AuthResult.Failure("Username or email already exists");
            }

            // Create new user
            var user = new User
            {
                Username = username,
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                DisplayName = username,
                IsActive = true
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Registration does not sign the user in; they log in explicitly afterwards
            _logger.LogInformation("User {Username} registered successfully", username);

            return AuthResult.Success(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration for user {Username}", username);
            return AuthResult.Failure("An error occurred during registration. Please try again.");
        }
    }

    private static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    public async Task<AuthResult> RestoreSessionAsync()
    {
        try
        {
            var saved = _sessionStore.Load();
            if (saved == null || string.IsNullOrEmpty(saved.Token))
            {
                return AuthResult.Failure("No saved session");
            }

            await using var context = _contextFactory.CreateDbContext();
            var user = await context.Users.FirstOrDefaultAsync(u => u.Id == saved.UserId && u.IsActive);

            var valid = user?.RememberTokenHash != null
                        && user.RememberTokenExpiresAt > DateTime.UtcNow
                        && CryptographicOperations.FixedTimeEquals(
                            Encoding.UTF8.GetBytes(user.RememberTokenHash),
                            Encoding.UTF8.GetBytes(HashToken(saved.Token)));

            if (!valid || user == null)
            {
                _logger.LogInformation("Saved session is invalid or expired");
                _sessionStore.Clear();
                return AuthResult.Failure("Saved session expired");
            }

            // Sliding expiry: using the app keeps the session alive
            user.RememberTokenExpiresAt = DateTime.UtcNow.Add(RememberMeLifetime);
            await context.SaveChangesAsync();

            _currentUser = user;
            var result = AuthResult.Success(user);
            AuthenticationChanged?.Invoke(this, result);
            _logger.LogInformation("Restored saved session for user {Username}", user.Username);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restoring saved session");
            return AuthResult.Failure("Could not restore the saved session");
        }
    }

    public async Task EnableRememberMeAsync()
    {
        if (_currentUser == null) return;

        try
        {
            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

            await using var context = _contextFactory.CreateDbContext();
            var user = await context.Users.FirstAsync(u => u.Id == _currentUser.Id);
            user.RememberTokenHash = HashToken(token);
            user.RememberTokenExpiresAt = DateTime.UtcNow.Add(RememberMeLifetime);
            await context.SaveChangesAsync();

            _sessionStore.Save(new RememberedSession { UserId = user.Id, Token = token });
            _logger.LogInformation("Remember-me session enabled for user {Username}", user.Username);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enabling remember me");
        }
    }

    public async Task ClearRememberedSessionAsync()
    {
        _sessionStore.Clear();
        if (_currentUser == null) return;

        try
        {
            await using var context = _contextFactory.CreateDbContext();
            var user = await context.Users.FirstOrDefaultAsync(u => u.Id == _currentUser.Id);
            if (user != null && user.RememberTokenHash != null)
            {
                user.RememberTokenHash = null;
                user.RememberTokenExpiresAt = null;
                await context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing remembered session");
        }
    }

    public async Task LogoutAsync()
    {
        try
        {
            await ClearRememberedSessionAsync();

            var username = _currentUser?.Username ?? "Unknown";
            _currentUser = null;
            
            AuthenticationChanged?.Invoke(this, AuthResult.Failure("Logged out"));
            _logger.LogInformation("User {Username} logged out", username);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
        }
        
        await Task.CompletedTask;
    }

    public async Task<bool> ChangePasswordAsync(string currentPassword, string newPassword)
    {
        try
        {
            await using var _context = _contextFactory.CreateDbContext();
            if (_currentUser == null || _currentUser.IsOAuthUser)
            {
                return false;
            }

            if (string.IsNullOrEmpty(_currentUser.PasswordHash) || 
                !BCrypt.Net.BCrypt.Verify(currentPassword, _currentUser.PasswordHash))
            {
                _logger.LogWarning("Password change failed: Invalid current password for user {Username}", _currentUser.Username);
                return false;
            }

            var dbUser = await _context.Users.FirstAsync(u => u.Id == _currentUser.Id);
            dbUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            dbUser.RememberTokenHash = null;
            dbUser.RememberTokenExpiresAt = null;
            _sessionStore.Clear();
            dbUser.UpdateTimestamp();

            await _context.SaveChangesAsync();
            _currentUser = dbUser;
            
            _logger.LogInformation("Password changed successfully for user {Username}", _currentUser.Username);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password for user {Username}", _currentUser?.Username);
            return false;
        }
    }

    public async Task<bool> ValidateCredentialsAsync(string username, string password)
    {
        try
        {
            await using var _context = _contextFactory.CreateDbContext();
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username && u.IsActive);

            return user != null && 
                   !string.IsNullOrEmpty(user.PasswordHash) &&
                   BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating credentials for user {Username}", username);
            return false;
        }
    }

    public async Task RefreshCurrentUserAsync()
    {
        try
        {
            await using var _context = _contextFactory.CreateDbContext();
            if (_currentUser == null) return;

            var refreshedUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == _currentUser.Id);

            if (refreshedUser != null)
            {
                _currentUser = refreshedUser;
                _logger.LogDebug("Refreshed current user data for {Username}", _currentUser.Username);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing current user data");
        }
    }

    /// <summary>
    /// Adds password authentication to an existing user (useful for OAuth users who want password login)
    /// </summary>
    public async Task<bool> AddPasswordToUserAsync(string username, string password)
    {
        try
        {
            await using var _context = _contextFactory.CreateDbContext();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username && u.IsActive);
            if (user == null)
            {
                _logger.LogWarning("Cannot add password: User {Username} not found", username);
                return false;
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
            user.UpdateTimestamp();
            
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Password added successfully for user {Username}", username);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding password for user {Username}", username);
            return false;
        }
    }
}