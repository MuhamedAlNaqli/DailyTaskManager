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
using Microsoft.Extensions.Configuration;
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
        private readonly GoogleClientSecrets? _googleClientSecrets;
        private readonly ILogger<OAuthService> _logger;

        // Google OAuth uses dynamic loopback address as per official documentation
        private const string GoogleScope = "openid profile email";

        public OAuthService(ILogger<OAuthService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _googleClientSecrets = LoadGoogleClientSecrets(configuration);
        }

        public async Task<OAuthResult> AuthenticateGoogleAsync()
        {
            try
            {
                if (_googleClientSecrets == null)
                {
                    _logger.LogWarning("Google OAuth requested but no client credentials are configured");
                    return OAuthResult.Failure("Google login is not configured. Set OAuth:Google:ClientId and OAuth:Google:ClientSecret in appsettings.json or provide client_secret.json.");
                }

                _logger.LogInformation("Starting Google OAuth authentication");
                return await RealGoogleOAuthFlow(_googleClientSecrets);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Google authentication error: {Message}", ex.Message);
                return OAuthResult.Failure($"Google authentication failed: {ex.Message}");
            }
        }

        public Task<OAuthResult> AuthenticateMicrosoftAsync()
        {
            _logger.LogWarning("Microsoft OAuth requested but is not implemented");
            return Task.FromResult(OAuthResult.Failure("Microsoft login is not available yet."));
        }

        private GoogleClientSecrets? LoadGoogleClientSecrets(IConfiguration configuration)
        {
            try
            {
                var clientId = configuration["OAuth:Google:ClientId"];
                var clientSecret = configuration["OAuth:Google:ClientSecret"];
                if (IsConfigured(clientId) && IsConfigured(clientSecret))
                {
                    return new GoogleClientSecrets { ClientId = clientId!, ClientSecret = clientSecret! };
                }

                var possiblePaths = new[]
                {
                    Path.Combine(AppContext.BaseDirectory, "client_secret.json"),
                    Path.Combine(Directory.GetCurrentDirectory(), "client_secret.json"),
                };

                foreach (var path in possiblePaths)
                {
                    if (!File.Exists(path)) continue;

                    var json = File.ReadAllText(path);
                    var secrets = JsonSerializer.Deserialize<GoogleClientSecretsFile>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    if (secrets != null && IsConfigured(secrets.Installed.ClientId) && IsConfigured(secrets.Installed.ClientSecret))
                    {
                        return secrets.Installed;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load Google OAuth credentials");
            }

            return null;
        }

        private static bool IsConfigured(string? value) =>
            !string.IsNullOrWhiteSpace(value) && !value.Contains("_here", StringComparison.OrdinalIgnoreCase);


        private async Task<OAuthResult> RealGoogleOAuthFlow(GoogleClientSecrets secrets)
        {
            var state = Guid.NewGuid().ToString();
            var codeVerifier = GenerateCodeVerifier();
            var codeChallenge = GenerateCodeChallenge(codeVerifier);

            // Use loopback address with dynamic port as per Google's official documentation
            var port = GetAvailablePort();
            var redirectUri = $"http://127.0.0.1:{port}/";
            
            var authUrl = $"https://accounts.google.com/o/oauth2/v2/auth?" +
                         $"client_id={secrets.ClientId}&" +
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

                var query = System.Web.HttpUtility.ParseQueryString(context.Request.Url?.Query ?? string.Empty);
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
                        new KeyValuePair<string, string>("client_id", secrets.ClientId),
                        new KeyValuePair<string, string>("client_secret", secrets.ClientSecret),
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
                    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenData?.AccessToken ?? string.Empty);
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

                    if (userData == null || string.IsNullOrEmpty(userData.Email))
                    {
                        return OAuthResult.Failure("Google did not return an email address");
                    }

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
            var result = new StringBuilder(128);
            for (int i = 0; i < 128; i++)
            {
                result.Append(chars[RandomNumberGenerator.GetInt32(chars.Length)]);
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
        [System.Text.Json.Serialization.JsonPropertyName("client_id")]
        public string ClientId { get; set; } = string.Empty;
        [System.Text.Json.Serialization.JsonPropertyName("client_secret")]
        public string ClientSecret { get; set; } = string.Empty;
        [System.Text.Json.Serialization.JsonPropertyName("redirect_uris")]
        public string[] RedirectUris { get; set; } = Array.Empty<string>();
    }
    
    public class GoogleTokenResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;
        [System.Text.Json.Serialization.JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("token_type")]
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