using System;
using System.Threading.Tasks;
using DailyTaskManager.Core.Models;
using System.Diagnostics;

namespace DailyTaskManager.Core.Services
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly IUserService _userService;
        private User _currentUser;

        public AuthenticationService(IUserService userService)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        }

        public AuthenticationService() : this(new UserService())
        {
        }

        public bool IsAuthenticated => _currentUser != null;

        public User CurrentUser => _currentUser;

        public event EventHandler<User> UserLoggedIn;
        public event EventHandler UserLoggedOut;

        public async Task<AuthResult> LoginAsync(LoginRequest request)
        {
            try
            {
                if (request == null)
                    return AuthResult.Failure("Login request is required");

                if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                    return AuthResult.Failure("Username and password are required");

                Debug.WriteLine($"AuthenticationService: Attempting login for user: {request.Username}");

                // Try to find user by username or email
                var user = await _userService.GetUserByUsernameAsync(request.Username);
                if (user == null)
                {
                    user = await _userService.GetUserByEmailAsync(request.Username);
                }

                if (user == null)
                {
                    Debug.WriteLine("AuthenticationService: User not found");
                    return AuthResult.Failure("Invalid username or password");
                }

                if (!string.IsNullOrWhiteSpace(user.OAuthProvider))
                {
                    Debug.WriteLine($"AuthenticationService: User uses OAuth provider: {user.OAuthProvider}");
                    return AuthResult.Failure($"This account uses {user.OAuthProvider} authentication. Please use the {user.OAuthProvider} login button.");
                }

                var isValidPassword = await _userService.ValidatePasswordAsync(user, request.Password);
                if (!isValidPassword)
                {
                    Debug.WriteLine("AuthenticationService: Invalid password");
                    return AuthResult.Failure("Invalid username or password");
                }

                _currentUser = user;
                UserLoggedIn?.Invoke(this, user);
                
                Debug.WriteLine($"AuthenticationService: Login successful for user: {user.Username}");
                return AuthResult.Success(user);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"AuthenticationService: Login error: {ex.Message}");
                return AuthResult.Failure("An error occurred during login. Please try again.");
            }
        }

        public async Task<AuthResult> RegisterAsync(string username, string email, string password, string displayName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                    return AuthResult.Failure("Username, email, and password are required");

                Debug.WriteLine($"AuthenticationService: Attempting registration for user: {username}");

                // Check if username already exists
                var existingUserByUsername = await _userService.GetUserByUsernameAsync(username);
                if (existingUserByUsername != null)
                    return AuthResult.Failure("Username already exists");

                // Check if email already exists
                var existingUserByEmail = await _userService.GetUserByEmailAsync(email);
                if (existingUserByEmail != null)
                    return AuthResult.Failure("Email already exists");

                var user = new User
                {
                    Username = username,
                    Email = email,
                    DisplayName = displayName ?? username,
                    PasswordHash = UserService.HashPassword(password),
                    IsActive = true
                };

                var createdUser = await _userService.CreateUserAsync(user);
                _currentUser = createdUser;
                UserLoggedIn?.Invoke(this, createdUser);

                Debug.WriteLine($"AuthenticationService: Registration successful for user: {username}");
                return AuthResult.Success(createdUser);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"AuthenticationService: Registration error: {ex.Message}");
                return AuthResult.Failure("An error occurred during registration. Please try again.");
            }
        }

        public async Task<AuthResult> AuthenticateWithOAuthAsync(OAuthResult oAuthResult)
        {
            try
            {
                if (oAuthResult == null || !oAuthResult.IsSuccess)
                    return AuthResult.Failure(oAuthResult?.ErrorMessage ?? "OAuth authentication failed");

                Debug.WriteLine($"AuthenticationService: OAuth authentication for {oAuthResult.Email} via {oAuthResult.Provider}");

                // Try to find existing user by OAuth credentials
                var user = await _userService.GetUserByOAuthAsync(oAuthResult.Provider, oAuthResult.ProviderId);

                if (user == null)
                {
                    // Try to find user by email (in case they have a local account)
                    user = await _userService.GetUserByEmailAsync(oAuthResult.Email);
                    if (user != null)
                    {
                        // User exists with same email but different auth method
                        if (string.IsNullOrWhiteSpace(user.OAuthProvider))
                        {
                            return AuthResult.Failure($"An account with this email already exists. Please use username/password login or link your {oAuthResult.Provider} account.");
                        }
                        else
                        {
                            return AuthResult.Failure($"This email is already linked to a {user.OAuthProvider} account.");
                        }
                    }

                    // Create new OAuth user
                    user = await _userService.CreateOAuthUserAsync(oAuthResult.Email, oAuthResult.DisplayName, oAuthResult.Provider, oAuthResult.ProviderId);
                }

                _currentUser = user;
                UserLoggedIn?.Invoke(this, user);

                Debug.WriteLine($"AuthenticationService: OAuth authentication successful for user: {user.Username}");
                return AuthResult.Success(user);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"AuthenticationService: OAuth authentication error: {ex.Message}");
                return AuthResult.Failure("An error occurred during OAuth authentication. Please try again.");
            }
        }

        public async Task LogoutAsync()
        {
            if (_currentUser != null)
            {
                Debug.WriteLine($"AuthenticationService: Logging out user: {_currentUser.Username}");
                _currentUser = null;
                UserLoggedOut?.Invoke(this, EventArgs.Empty);
            }
            await Task.CompletedTask;
        }
    }
}