using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Bridgewater.DropboxLinker.Core.Contracts;

namespace Bridgewater.DropboxLinker.Core.Auth
{
    /// <summary>
    /// Manages Dropbox OAuth 2.0 authentication using PKCE flow.
    /// </summary>
    public sealed class DropboxAuthService : IDropboxAuthService, IDisposable
    {
        // Dropbox OAuth endpoints
        private const string AuthorizeUrl = "https://www.dropbox.com/oauth2/authorize";
        private const string TokenUrl = "https://api.dropboxapi.com/oauth2/token";
        
        // Local callback server
        private const string RedirectUri = "http://localhost:17823/callback";
        private const int CallbackPort = 17823;

        private readonly string _appKey;
        private readonly SecureTokenStorage _tokenStorage;
        private readonly IAppLogger _logger;
        private readonly HttpClient _httpClient;

        private string? _accessToken;
        private DateTimeOffset _accessTokenExpiry = DateTimeOffset.MinValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="DropboxAuthService"/> class.
        /// </summary>
        /// <param name="appKey">The Dropbox app key.</param>
        /// <param name="tokenStorage">The token storage service.</param>
        /// <param name="logger">The logger.</param>
        public DropboxAuthService(string appKey, SecureTokenStorage tokenStorage, IAppLogger logger)
        {
            _appKey = appKey ?? throw new ArgumentNullException(nameof(appKey));
            _tokenStorage = tokenStorage ?? throw new ArgumentNullException(nameof(tokenStorage));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpClient = new HttpClient();
        }

        /// <inheritdoc />
        public async Task<object> GetClientAsync(CancellationToken ct)
        {
            // Check if we have a valid access token
            if (!string.IsNullOrEmpty(_accessToken) && DateTimeOffset.UtcNow < _accessTokenExpiry)
            {
                return _accessToken;
            }

            // Try to refresh using stored token
            var refreshToken = _tokenStorage.GetRefreshToken();
            if (!string.IsNullOrEmpty(refreshToken))
            {
                try
                {
                    await RefreshAccessTokenAsync(refreshToken, ct);
                    if (!string.IsNullOrEmpty(_accessToken))
                    {
                        return _accessToken;
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warn($"Token refresh failed: {ex.Message}");
                }
            }

            // Need fresh authentication
            await AuthenticateAsync(ct);
            return _accessToken ?? throw new InvalidOperationException("Authentication failed.");
        }

        /// <inheritdoc />
        public async Task ReauthenticateAsync(CancellationToken ct)
        {
            _tokenStorage.DeleteRefreshToken();
            _accessToken = null;
            _accessTokenExpiry = DateTimeOffset.MinValue;
            await AuthenticateAsync(ct);
        }

        /// <summary>
        /// Performs the OAuth 2.0 PKCE flow.
        /// </summary>
        private async Task AuthenticateAsync(CancellationToken ct)
        {
            _logger.Info("Starting Dropbox OAuth authentication...");

            // Generate PKCE values
            var codeVerifier = GenerateCodeVerifier();
            var codeChallenge = GenerateCodeChallenge(codeVerifier);
            var state = GenerateState();

            // Build authorization URL
            var authUrl = $"{AuthorizeUrl}" +
                $"?client_id={Uri.EscapeDataString(_appKey)}" +
                $"&response_type=code" +
                $"&redirect_uri={Uri.EscapeDataString(RedirectUri)}" +
                $"&code_challenge={Uri.EscapeDataString(codeChallenge)}" +
                $"&code_challenge_method=S256" +
                $"&state={Uri.EscapeDataString(state)}" +
                $"&token_access_type=offline";

            // Start local HTTP listener for callback
            using var listener = new HttpListener();
            listener.Prefixes.Add($"http://localhost:{CallbackPort}/");
            listener.Start();

            // Open browser for user authentication
            Process.Start(new ProcessStartInfo
            {
                FileName = authUrl,
                UseShellExecute = true
            });

            _logger.Info("Waiting for user to complete Dropbox authentication...");

            // Wait for callback
            var context = await listener.GetContextAsync();
            var queryString = context.Request.Url?.Query ?? string.Empty;
            var queryParams = ParseQueryString(queryString);

            // Send response to browser
            var responseHtml = GetCallbackResponseHtml(queryParams.ContainsKey("code"));
            var responseBytes = Encoding.UTF8.GetBytes(responseHtml);
            context.Response.ContentType = "text/html";
            context.Response.ContentLength64 = responseBytes.Length;
            await context.Response.OutputStream.WriteAsync(responseBytes, 0, responseBytes.Length, ct);
            context.Response.Close();

            listener.Stop();

            // Validate state
            if (!queryParams.TryGetValue("state", out var returnedState) || returnedState != state)
            {
                throw new InvalidOperationException("OAuth state mismatch. Authentication may have been intercepted.");
            }

            // Get authorization code
            if (!queryParams.TryGetValue("code", out var code))
            {
                var error = queryParams.TryGetValue("error", out var err) ? err : "Unknown error";
                throw new InvalidOperationException($"Dropbox authentication failed: {error}");
            }

            // Exchange code for tokens
            await ExchangeCodeForTokensAsync(code, codeVerifier, ct);
            _logger.Info("Dropbox authentication completed successfully.");
        }

        /// <summary>
        /// Exchanges the authorization code for access and refresh tokens.
        /// </summary>
        private async Task ExchangeCodeForTokensAsync(string code, string codeVerifier, CancellationToken ct)
        {
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["code"] = code,
                ["grant_type"] = "authorization_code",
                ["redirect_uri"] = RedirectUri,
                ["code_verifier"] = codeVerifier,
                ["client_id"] = _appKey
            });

            var response = await _httpClient.PostAsync(TokenUrl, content, ct);
            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Token exchange failed: {json}");
            }

            ProcessTokenResponse(json);
        }

        /// <summary>
        /// Refreshes the access token using the refresh token.
        /// </summary>
        private async Task RefreshAccessTokenAsync(string refreshToken, CancellationToken ct)
        {
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["refresh_token"] = refreshToken,
                ["grant_type"] = "refresh_token",
                ["client_id"] = _appKey
            });

            var response = await _httpClient.PostAsync(TokenUrl, content, ct);
            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Token refresh failed: {json}");
            }

            ProcessTokenResponse(json);
        }

        /// <summary>
        /// Processes the token response JSON and stores tokens.
        /// </summary>
        private void ProcessTokenResponse(string json)
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("access_token", out var accessTokenProp))
            {
                _accessToken = accessTokenProp.GetString();
            }

            if (root.TryGetProperty("expires_in", out var expiresInProp))
            {
                var expiresIn = expiresInProp.GetInt32();
                // Subtract 5 minutes for safety margin
                _accessTokenExpiry = DateTimeOffset.UtcNow.AddSeconds(expiresIn - 300);
            }

            if (root.TryGetProperty("refresh_token", out var refreshTokenProp))
            {
                var refreshToken = refreshTokenProp.GetString();
                if (!string.IsNullOrEmpty(refreshToken))
                {
                    _tokenStorage.StoreRefreshToken(refreshToken);
                }
            }
        }

        /// <summary>
        /// Generates a cryptographically random code verifier for PKCE.
        /// </summary>
        private static string GenerateCodeVerifier()
        {
            var bytes = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return Base64UrlEncode(bytes);
        }

        /// <summary>
        /// Generates the S256 code challenge from the verifier.
        /// </summary>
        private static string GenerateCodeChallenge(string codeVerifier)
        {
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(Encoding.ASCII.GetBytes(codeVerifier));
            return Base64UrlEncode(hash);
        }

        /// <summary>
        /// Generates a random state value for CSRF protection.
        /// </summary>
        private static string GenerateState()
        {
            var bytes = new byte[16];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return Base64UrlEncode(bytes);
        }

        /// <summary>
        /// Base64URL encodes bytes without padding.
        /// </summary>
        private static string Base64UrlEncode(byte[] bytes)
        {
            return Convert.ToBase64String(bytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        /// <summary>
        /// Parses a query string into a dictionary.
        /// </summary>
        private static Dictionary<string, string> ParseQueryString(string queryString)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrEmpty(queryString))
            {
                return result;
            }

            var query = queryString.TrimStart('?');
            foreach (var pair in query.Split('&'))
            {
                var parts = pair.Split('=');
                if (parts.Length == 2)
                {
                    result[Uri.UnescapeDataString(parts[0])] = Uri.UnescapeDataString(parts[1]);
                }
            }

            return result;
        }

        /// <summary>
        /// Returns HTML for the OAuth callback response.
        /// </summary>
        private static string GetCallbackResponseHtml(bool success)
        {
            var message = success 
                ? "Authentication successful! You can close this window." 
                : "Authentication failed. Please try again.";
            var color = success ? "#28a745" : "#dc3545";

            return $@"<!DOCTYPE html>
<html>
<head>
    <title>Dropbox Authentication</title>
    <style>
        body {{ 
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
            display: flex; 
            justify-content: center; 
            align-items: center; 
            height: 100vh; 
            margin: 0;
            background: #f5f5f5;
        }}
        .message {{ 
            text-align: center; 
            padding: 40px; 
            background: white;
            border-radius: 12px;
            box-shadow: 0 2px 8px rgba(0,0,0,0.1);
        }}
        .icon {{ font-size: 48px; margin-bottom: 16px; }}
        .text {{ color: {color}; font-size: 18px; }}
    </style>
</head>
<body>
    <div class='message'>
        <div class='icon'>{(success ? "✓" : "✗")}</div>
        <div class='text'>{message}</div>
    </div>
</body>
</html>";
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _httpClient.Dispose();
        }
    }
}
