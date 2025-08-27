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
using DailyTaskManager.Core.Models;
using Newtonsoft.Json;
using System.Reflection;

namespace DailyTaskManager.Core.Services
{
    public class OAuthService : IOAuthService
    {
        private static GoogleClientSecrets _googleClientSecrets;
        // Google OAuth uses dynamic loopback address as per official documentation
        private const string GoogleScope = "openid profile email";

        private const string MicrosoftClientId = "your-microsoft-client-id";
        private const string MicrosoftClientSecret = "your-microsoft-client-secret";
        private const string MicrosoftRedirectUri = "http://localhost:8080/oauth/callback";
        private const string MicrosoftScope = "openid profile email";

        static OAuthService()
        {
            LoadGoogleClientSecrets();
        }

        public async Task<OAuthResult> AuthenticateGoogleAsync()
        {
            try
            {
                if (_googleClientSecrets == null)
                {
                    return OAuthResult.Failure("Google OAuth configuration not found");
                }

                return await RealGoogleOAuthFlow();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"OAuthService: Google authentication error: {ex.Message}");
                return OAuthResult.Failure($"Google authentication failed: {ex.Message}");
            }
        }

        public async Task<OAuthResult> AuthenticateMicrosoftAsync()
        {
            try
            {
                return await SimulateOAuthFlow("Microsoft");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"OAuthService: Microsoft authentication error: {ex.Message}");
                return OAuthResult.Failure("Microsoft authentication failed");
            }
        }

        private async Task<OAuthResult> SimulateOAuthFlow(string provider)
        {
            // This is a demonstration implementation
            // In production, you would integrate with actual OAuth providers

            await Task.Delay(1000); // Simulate network delay

            // For demonstration, create a mock user
            var mockUser = new User
            {
                Username = $"{provider.ToLower()}user_{DateTime.Now.Ticks}",
                Email = $"user@{provider.ToLower()}.com",
                DisplayName = $"{provider} User",
                OAuthProvider = provider,
                OAuthId = Guid.NewGuid().ToString(),
                IsActive = true
            };

            return OAuthResult.Success(mockUser.Email, mockUser.DisplayName, mockUser.OAuthId, provider);
        }

        private static void LoadGoogleClientSecrets()
        {
            try
            {
                var assemblyLocation = Assembly.GetExecutingAssembly().Location;
                var assemblyDirectory = Path.GetDirectoryName(assemblyLocation);
                var credentialsPath = Path.Combine(assemblyDirectory, "client_secret.json");
                
                if (!File.Exists(credentialsPath))
                {
                    // Try alternative path in project directory
                    var projectPath = Path.GetFullPath(Path.Combine(assemblyDirectory, @"..\..\..\DailyTaskmanager.Core\client_secret.json"));
                    if (File.Exists(projectPath))
                    {
                        credentialsPath = projectPath;
                    }
                    else
                    {
                        Debug.WriteLine("Google client secrets file not found");
                        return;
                    }
                }

                var json = File.ReadAllText(credentialsPath);
                var secrets = JsonConvert.DeserializeObject<GoogleClientSecretsFile>(json);
                _googleClientSecrets = secrets.Installed;
                
                Debug.WriteLine("Google OAuth credentials loaded successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to load Google OAuth credentials: {ex.Message}");
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
                         $"redirect_uri={HttpUtility.UrlEncode(redirectUri.TrimEnd('/'))}&" +
                         $"response_type=code&" +
                         $"scope={HttpUtility.UrlEncode(GoogleScope)}&" +
                         $"state={state}&" +
                         $"code_challenge={codeChallenge}&" +
                         $"code_challenge_method=S256";

            // Start local HTTP server on loopback address with dynamic port
            var listener = new HttpListener();
            listener.Prefixes.Add(redirectUri);
            
            try
            {
                listener.Start();
                Debug.WriteLine($"OAuth callback server started on {redirectUri}");

                // Open browser
                Process.Start(new ProcessStartInfo(authUrl) { UseShellExecute = true });
                Debug.WriteLine($"Browser opened for OAuth: {authUrl}");

                // Wait for callback with timeout
                var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
                var context = await GetHttpContextAsync(listener, cts.Token);
                
                if (context == null)
                {
                    return OAuthResult.Failure("OAuth authentication timed out");
                }

                var query = HttpUtility.ParseQueryString(context.Request.Url.Query);
                var code = query["code"];
                var returnedState = query["state"];
                var error = query["error"];

                // Send response to browser
                await SendBrowserResponse(context.Response, error == null, error);

                if (!string.IsNullOrEmpty(error))
                {
                    return OAuthResult.Failure($"OAuth error: {error}");
                }

                if (string.IsNullOrEmpty(code))
                {
                    return OAuthResult.Failure("Authorization code not received");
                }

                if (returnedState != state)
                {
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
                    
                    Debug.WriteLine($"Token response: {tokenContent}");
                    
                    if (!tokenResponse.IsSuccessStatusCode)
                    {
                        return OAuthResult.Failure($"Token exchange failed: {tokenContent}");
                    }

                    var tokenData = JsonConvert.DeserializeObject<GoogleTokenResponse>(tokenContent);

                    // Get user info
                    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenData.AccessToken);
                    var userResponse = await client.GetAsync("https://www.googleapis.com/oauth2/v2/userinfo");
                    var userContent = await userResponse.Content.ReadAsStringAsync();
                    
                    if (!userResponse.IsSuccessStatusCode)
                    {
                        return OAuthResult.Failure($"Failed to get user info: {userContent}");
                    }
                    
                    var userData = JsonConvert.DeserializeObject<GoogleUserInfo>(userContent);

                    return OAuthResult.Success(userData.Email, userData.Name, userData.Id, "Google");
                }
            }
            finally
            {
                listener?.Stop();
                Debug.WriteLine("OAuth callback server stopped");
            }
        }

        private async Task<HttpListenerContext> GetHttpContextAsync(HttpListener listener, CancellationToken cancellationToken)
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
                Debug.WriteLine($"Error getting HTTP context: {ex.Message}");
                return null;
            }
        }

        private async Task SendBrowserResponse(HttpListenerResponse response, bool success, string error = null)
        {
            var html = success 
                ? "<html><body><h1>Authentication Successful!</h1><p>You can close this window and return to the application.</p></body></html>"
                : $"<html><body><h1>Authentication Failed</h1><p>Error: {error}</p><p>You can close this window and try again.</p></body></html>";
            
            var buffer = Encoding.UTF8.GetBytes(html);
            response.ContentLength64 = buffer.Length;
            response.ContentType = "text/html; charset=utf-8";
            
            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            response.OutputStream.Close();
        }

        private int GetAvailablePort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        private string GenerateCodeVerifier()
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

        private string GenerateCodeChallenge(string codeVerifier)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                var challengeBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(codeVerifier));
                return Convert.ToBase64String(challengeBytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
            }
        }
    }

    // Helper classes for OAuth implementation
    public class GoogleClientSecretsFile
    {
        [JsonProperty("installed")]
        public GoogleClientSecrets Installed { get; set; }
    }

    public class GoogleClientSecrets
    {
        [JsonProperty("client_id")]
        public string ClientId { get; set; }
        
        [JsonProperty("client_secret")]
        public string ClientSecret { get; set; }
        
        [JsonProperty("redirect_uris")]
        public string[] RedirectUris { get; set; }
    }
    
    public class GoogleTokenResponse
    {
        [JsonProperty("access_token")]
        public string AccessToken { get; set; }
        
        [JsonProperty("refresh_token")]
        public string RefreshToken { get; set; }
        
        [JsonProperty("expires_in")]
        public int ExpiresIn { get; set; }
        
        [JsonProperty("token_type")]
        public string TokenType { get; set; }
    }

    public class GoogleUserInfo
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        
        [JsonProperty("email")]
        public string Email { get; set; }
        
        [JsonProperty("name")]
        public string Name { get; set; }
        
        [JsonProperty("picture")]
        public string Picture { get; set; }
    }
}