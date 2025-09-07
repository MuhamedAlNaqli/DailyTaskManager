using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Text.Json;
using System.Reflection;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using DailyTaskManager.Core.Interfaces;
using DailyTaskManager.Core.Models;

namespace DailyTaskManager.Infrastructure.Services
{
    /// <summary>
    /// OAuth authentication service for Google and Microsoft
    /// </summary>
    public class OAuthService : IOAuthService
    {
        private static GoogleClientSecrets? _googleClientSecrets;
        private readonly ILogger<OAuthService> _logger;
        
        // Google OAuth uses dynamic loopback address as per official documentation
        private const string GoogleScope = "openid profile email";

        static OAuthService()
        {
            LoadGoogleClientSecrets();
        }

        public OAuthService(ILogger<OAuthService> logger)
        {
            _logger = logger;
        }

        public async Task<OAuthResult> AuthenticateGoogleAsync()
        {
            try
            {
                _logger.LogInformation("Starting Google OAuth authentication");
                
                // For development purposes, use simulated flow since credentials may not be configured
                _logger.LogInformation("Using simulated Google OAuth for development");
                return await SimulateOAuthFlow("Google");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Google authentication error: {Message}", ex.Message);
                return OAuthResult.Failure($"Google authentication failed: {ex.Message}");
            }
        }

        public async Task<OAuthResult> AuthenticateMicrosoftAsync()
        {
            try
            {
                _logger.LogInformation("Microsoft OAuth not yet implemented, using simulation");
                return await SimulateOAuthFlow("Microsoft");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Microsoft authentication error: {Message}", ex.Message);
                return OAuthResult.Failure("Microsoft authentication failed");
            }
        }

        private async Task<OAuthResult> SimulateOAuthFlow(string provider)
        {
            // This is a demonstration implementation for testing
            // In production, you would integrate with actual OAuth providers

            await Task.Delay(1500); // Simulate network delay

            // Create a realistic test user for demonstration
            var mockEmail = provider.ToLower() == "google" ? "testuser@gmail.com" : $"testuser@{provider.ToLower()}.com";
            var mockDisplayName = provider.ToLower() == "google" ? "Test User (Google)" : $"Test User ({provider})";
            var mockId = provider.ToLower() == "google" ? "google_test_123" : Guid.NewGuid().ToString();

            return OAuthResult.Success(mockEmail, mockDisplayName, mockId, provider);
        }

        private static void LoadGoogleClientSecrets()
        {
            try
            {
                // Try to load from multiple possible locations
                var possiblePaths = new[]
                {
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "client_secret.json"),
                    Path.Combine(Directory.GetCurrentDirectory(), "client_secret.json"),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "client_secret.json"),
                };

                string credentialsPath = null;
                foreach (var path in possiblePaths)
                {
                    if (File.Exists(path))
                    {
                        credentialsPath = path;
                        break;
                    }
                }

                if (credentialsPath == null)
                {
                    // Create a default configuration for development
                    _googleClientSecrets = new GoogleClientSecrets
                    {
                        ClientId = "99933633137-tl7uoat7rq6bb12q4ctqlvtt3uif8joa.apps.googleusercontent.com",
                        ClientSecret = "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",
                        RedirectUris = new[] { "http://127.0.0.1" }
                    };
                    Debug.WriteLine("Using default Google OAuth credentials for development");
                    return;
                }

                var json = File.ReadAllText(credentialsPath);
                var secrets = JsonSerializer.Deserialize<GoogleClientSecretsFile>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                _googleClientSecrets = secrets.Installed;
                
                Debug.WriteLine($"Google OAuth credentials loaded successfully from {credentialsPath}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to load Google OAuth credentials: {ex.Message}");
                
                // Fallback to default configuration
                _googleClientSecrets = new GoogleClientSecrets
                {
                    ClientId = "99933633137-tl7uoat7rq6bb12q4ctqlvtt3uif8joa.apps.googleusercontent.com",
                    ClientSecret = "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",
                    RedirectUris = new[] { "http://127.0.0.1" }
                };
                Debug.WriteLine("Using fallback Google OAuth credentials");
            }
        }

        private async Task<OAuthResult> RealGoogleOAuthFlow()
        {
            var state = Guid.NewGuid().ToString();
            var codeVerifier = GenerateCodeVerifier();
            var codeChallenge = GenerateCodeChallenge(codeVerifier);

            // Use loopback address with dynamic port as per Google's official documentation
            var port = GetAvailablePort();
            var redirectUri = $"http://127.0.0.1:{port}/";
            
            var authUrl = $"https://accounts.google.com/o/oauth2/v2/auth?" +
                         $"client_id={_googleClientSecrets.ClientId}&" +
                         $"redirect_uri={Uri.EscapeDataString(redirectUri.TrimEnd('/'))}&" +
                         $"response_type=code&" +
                         $"scope={Uri.EscapeDataString(GoogleScope)}&" +
                         $"state={state}&" +
                         $"code_challenge={codeChallenge}&" +
                         $"code_challenge_method=S256";

            // Start local HTTP server on loopback address with dynamic port
            var listener = new HttpListener();
            listener.Prefixes.Add(redirectUri);
            
            try
            {
                listener.Start();
                _logger.LogDebug("OAuth callback server started on {RedirectUri}", redirectUri);

                // Open browser
                var startInfo = new ProcessStartInfo(authUrl)
                {
                    UseShellExecute = true,
                    Verb = "open"
                };
                Process.Start(startInfo);
                _logger.LogDebug("Browser opened for OAuth: {AuthUrl}", authUrl);

                // Wait for callback with timeout
                var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
                var context = await GetHttpContextAsync(listener, cts.Token);
                
                if (context == null)
                {
                    _logger.LogWarning("OAuth authentication timed out");
                    return OAuthResult.Failure("OAuth authentication timed out");
                }

                var query = System.Web.HttpUtility.ParseQueryString(context.Request.Url.Query);
                var code = query["code"];
                var returnedState = query["state"];
                var error = query["error"];

                // Send response to browser
                await SendBrowserResponse(context.Response, error == null, error);

                if (!string.IsNullOrEmpty(error))
                {
                    _logger.LogError("OAuth error: {Error}", error);
                    return OAuthResult.Failure($"OAuth error: {error}");
                }

                if (string.IsNullOrEmpty(code))
                {
                    _logger.LogError("Authorization code not received");
                    return OAuthResult.Failure("Authorization code not received");
                }

                if (returnedState != state)
                {
                    _logger.LogError("Invalid state parameter");
                    return OAuthResult.Failure("Invalid state parameter");
                }

                // Exchange code for tokens
                using (var client = new HttpClient())
                {
                    var tokenRequest = new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("client_id", _googleClientSecrets.ClientId),
                        new KeyValuePair<string, string>("client_secret", _googleClientSecrets.ClientSecret),
                        new KeyValuePair<string, string>("code", code),
                        new KeyValuePair<string, string>("grant_type", "authorization_code"),
                        new KeyValuePair<string, string>("redirect_uri", redirectUri.TrimEnd('/')),
                        new KeyValuePair<string, string>("code_verifier", codeVerifier)
                    });

                    var tokenResponse = await client.PostAsync("https://oauth2.googleapis.com/token", tokenRequest);
                    var tokenContent = await tokenResponse.Content.ReadAsStringAsync();
                    
                    _logger.LogDebug("Token response status: {StatusCode}", tokenResponse.StatusCode);
                    
                    if (!tokenResponse.IsSuccessStatusCode)
                    {
                        _logger.LogError("Token exchange failed: {Content}", tokenContent);
                        return OAuthResult.Failure($"Token exchange failed: {tokenContent}");
                    }

                    var tokenData = JsonSerializer.Deserialize<GoogleTokenResponse>(tokenContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    // Get user info
                    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenData.AccessToken);
                    var userResponse = await client.GetAsync("https://www.googleapis.com/oauth2/v2/userinfo");
                    var userContent = await userResponse.Content.ReadAsStringAsync();
                    
                    if (!userResponse.IsSuccessStatusCode)
                    {
                        _logger.LogError("Failed to get user info: {Content}", userContent);
                        return OAuthResult.Failure($"Failed to get user info: {userContent}");
                    }
                    
                    var userData = JsonSerializer.Deserialize<GoogleUserInfo>(userContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    _logger.LogInformation("Successfully authenticated user: {Email}", userData.Email);
                    return OAuthResult.Success(userData.Email, userData.Name, userData.Id, "Google");
                }
            }
            finally
            {
                listener?.Stop();
                _logger.LogDebug("OAuth callback server stopped");
            }
        }

        private async Task<HttpListenerContext?> GetHttpContextAsync(HttpListener listener, CancellationToken cancellationToken)
        {
            try
            {
                var contextTask = listener.GetContextAsync();
                var completedTask = await Task.WhenAny(contextTask, Task.Delay(Timeout.Infinite, cancellationToken));
                
                if (completedTask == contextTask)
                {
                    return await contextTask;
                }
                
                return null; // Timeout
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting HTTP context: {Message}", ex.Message);
                return null;
            }
        }

        private async Task SendBrowserResponse(HttpListenerResponse response, bool success, string? error = null)
        {
            var html = success 
                ? """
                  <html>
                  <head><title>Authentication Successful</title></head>
                  <body style="font-family: Arial, sans-serif; text-align: center; padding: 50px;">
                      <h1 style="color: green;">✓ Authentication Successful!</h1>
                      <p>You can close this window and return to the Daily Task Manager application.</p>
                      <script>setTimeout(() => window.close(), 2000);</script>
                  </body>
                  </html>
                  """
                : $"""
                  <html>
                  <head><title>Authentication Failed</title></head>
                  <body style="font-family: Arial, sans-serif; text-align: center; padding: 50px;">
                      <h1 style="color: red;">✗ Authentication Failed</h1>
                      <p>Error: {error}</p>
                      <p>You can close this window and try again in the application.</p>
                  </body>
                  </html>
                  """;
            
            var buffer = Encoding.UTF8.GetBytes(html);
            response.ContentLength64 = buffer.Length;
            response.ContentType = "text/html; charset=utf-8";
            
            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            response.OutputStream.Close();
        }

        private static int GetAvailablePort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        private static string GenerateCodeVerifier()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-._~";
            var random = new Random();
            var result = new StringBuilder(128);
            for (int i = 0; i < 128; i++)
            {
                result.Append(chars[random.Next(chars.Length)]);
            }
            return result.ToString();
        }

        private static string GenerateCodeChallenge(string codeVerifier)
        {
            using var sha256 = SHA256.Create();
            var challengeBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(codeVerifier));
            return Convert.ToBase64String(challengeBytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }
    }

    // Helper classes for OAuth implementation
    public class GoogleClientSecretsFile
    {
        public GoogleClientSecrets Installed { get; set; } = new();
    }

    public class GoogleClientSecrets
    {
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string[] RedirectUris { get; set; } = Array.Empty<string>();
    }
    
    public class GoogleTokenResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public string? RefreshToken { get; set; }
        public int ExpiresIn { get; set; }
        public string TokenType { get; set; } = string.Empty;
    }

    public class GoogleUserInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Picture { get; set; }
    }
}