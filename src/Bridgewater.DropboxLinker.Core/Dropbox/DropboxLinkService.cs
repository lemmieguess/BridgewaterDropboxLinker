using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Bridgewater.DropboxLinker.Core.Contracts;
using Bridgewater.DropboxLinker.Core.Utilities;

namespace Bridgewater.DropboxLinker.Core.Dropbox
{
    /// <summary>
    /// Creates and manages Dropbox shared links via the Dropbox API.
    /// </summary>
    public sealed class DropboxLinkService : IDropboxLinkService, IDisposable
    {
        private const string CreateSharedLinkUrl = "https://api.dropboxapi.com/2/sharing/create_shared_link_with_settings";
        private const string ListSharedLinksUrl = "https://api.dropboxapi.com/2/sharing/list_shared_links";

        private readonly IDropboxAuthService _authService;
        private readonly IDropboxFolderLocator _folderLocator;
        private readonly IDropboxPathMapper _pathMapper;
        private readonly IAppLogger _logger;
        private readonly HttpClient _httpClient;
        private readonly string? _rootNamespaceId;

        /// <summary>
        /// Initializes a new instance of the <see cref="DropboxLinkService"/> class.
        /// </summary>
        /// <param name="authService">The authentication service.</param>
        /// <param name="folderLocator">The folder locator.</param>
        /// <param name="pathMapper">The path mapper.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="rootNamespaceId">Optional team space root namespace ID.</param>
        public DropboxLinkService(
            IDropboxAuthService authService,
            IDropboxFolderLocator folderLocator,
            IDropboxPathMapper pathMapper,
            IAppLogger logger,
            string? rootNamespaceId = null)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _folderLocator = folderLocator ?? throw new ArgumentNullException(nameof(folderLocator));
            _pathMapper = pathMapper ?? throw new ArgumentNullException(nameof(pathMapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _rootNamespaceId = rootNamespaceId;
            _httpClient = new HttpClient();
        }

        /// <inheritdoc />
        public async Task<LinkResult> CreateOrReuseSharedLinkAsync(LinkRequest req, CancellationToken ct)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (string.IsNullOrEmpty(req.LocalFilePath))
            {
                throw new ArgumentException("Local file path is required.", nameof(req));
            }

            _logger.Info($"Creating shared link for: {Path.GetFileName(req.LocalFilePath)}");

            // Get Dropbox root and convert path
            var dropboxRoot = _folderLocator.GetBusinessDropboxRoot();
            var dropboxPath = _pathMapper.ToDropboxPath(dropboxRoot, req.LocalFilePath);

            _logger.Info($"Dropbox path: {dropboxPath}");

            // Get access token
            var accessToken = (string)await _authService.GetClientAsync(ct);

            // Try to create a new shared link
            try
            {
                var url = await CreateSharedLinkAsync(accessToken, dropboxPath, req.ExpiresAtUtc, ct);
                var displayName = FileNameCleaner.CleanForDisplay(Path.GetFileName(req.LocalFilePath));

                _logger.Info($"Shared link created successfully: {url}");

                return new LinkResult
                {
                    Url = url,
                    DisplayName = displayName,
                    DropboxPath = dropboxPath,
                    ReusedExisting = false
                };
            }
            catch (DropboxApiException ex) when (ex.ErrorTag == "shared_link_already_exists")
            {
                _logger.Info("Shared link already exists, retrieving existing link...");
                
                // Get existing link
                var url = await GetExistingSharedLinkAsync(accessToken, dropboxPath, ct);
                var displayName = FileNameCleaner.CleanForDisplay(Path.GetFileName(req.LocalFilePath));

                return new LinkResult
                {
                    Url = url,
                    DisplayName = displayName,
                    DropboxPath = dropboxPath,
                    ReusedExisting = true
                };
            }
        }

        /// <summary>
        /// Creates a new shared link via the Dropbox API.
        /// </summary>
        private async Task<string> CreateSharedLinkAsync(
            string accessToken, 
            string dropboxPath, 
            DateTimeOffset expiresAt,
            CancellationToken ct)
        {
            var requestBody = new
            {
                path = dropboxPath,
                settings = new
                {
                    requested_visibility = "public",
                    expires = expiresAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    audience = "public"
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var request = new HttpRequestMessage(HttpMethod.Post, CreateSharedLinkUrl)
            {
                Content = content
            };
            request.Headers.Add("Authorization", $"Bearer {accessToken}");

            // Add path root header for team spaces
            if (!string.IsNullOrEmpty(_rootNamespaceId))
            {
                var pathRoot = new System.Collections.Generic.Dictionary<string, string>
                {
                    { ".tag", "root" },
                    { "root", _rootNamespaceId }
                };
                request.Headers.Add("Dropbox-API-Path-Root", JsonSerializer.Serialize(pathRoot));
            }

            var response = await _httpClient.SendAsync(request, ct);
            var responseJson = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                HandleApiError(responseJson, response.StatusCode);
            }

            // Parse response to get URL
            using var doc = JsonDocument.Parse(responseJson);
            if (doc.RootElement.TryGetProperty("url", out var urlProp))
            {
                return urlProp.GetString() ?? throw new InvalidOperationException("No URL in response");
            }

            throw new InvalidOperationException("Failed to parse shared link response");
        }

        /// <summary>
        /// Gets an existing shared link for the specified path.
        /// </summary>
        private async Task<string> GetExistingSharedLinkAsync(
            string accessToken,
            string dropboxPath,
            CancellationToken ct)
        {
            var requestBody = new
            {
                path = dropboxPath,
                direct_only = true
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var request = new HttpRequestMessage(HttpMethod.Post, ListSharedLinksUrl)
            {
                Content = content
            };
            request.Headers.Add("Authorization", $"Bearer {accessToken}");

            if (!string.IsNullOrEmpty(_rootNamespaceId))
            {
                var pathRoot = new System.Collections.Generic.Dictionary<string, string>
                {
                    { ".tag", "root" },
                    { "root", _rootNamespaceId }
                };
                request.Headers.Add("Dropbox-API-Path-Root", JsonSerializer.Serialize(pathRoot));
            }

            var response = await _httpClient.SendAsync(request, ct);
            var responseJson = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                HandleApiError(responseJson, response.StatusCode);
            }

            using var doc = JsonDocument.Parse(responseJson);
            if (doc.RootElement.TryGetProperty("links", out var links) && 
                links.GetArrayLength() > 0)
            {
                var firstLink = links[0];
                if (firstLink.TryGetProperty("url", out var urlProp))
                {
                    return urlProp.GetString() ?? throw new InvalidOperationException("No URL in response");
                }
            }

            throw new InvalidOperationException("No existing shared link found");
        }

        /// <summary>
        /// Handles API errors and throws appropriate exceptions.
        /// </summary>
        private void HandleApiError(string responseJson, System.Net.HttpStatusCode statusCode)
        {
            _logger.Error(new Exception(responseJson), $"Dropbox API error ({statusCode})");

            try
            {
                using var doc = JsonDocument.Parse(responseJson);
                
                // Check for error structure
                if (doc.RootElement.TryGetProperty("error", out var error))
                {
                    if (error.TryGetProperty(".tag", out var tagProp))
                    {
                        var errorTag = tagProp.GetString();
                        throw new DropboxApiException(errorTag ?? "unknown", responseJson);
                    }
                }

                // Check for error_summary
                if (doc.RootElement.TryGetProperty("error_summary", out var summary))
                {
                    var errorText = summary.GetString();
                    
                    // Parse error tag from summary (format: "error_tag/details")
                    var parts = errorText?.Split('/');
                    var errorTag = parts?.Length > 0 ? parts[0] : "unknown";
                    
                    throw new DropboxApiException(errorTag, errorText ?? responseJson);
                }
            }
            catch (JsonException)
            {
                // Response wasn't JSON
            }

            throw new InvalidOperationException($"Dropbox API error: {responseJson}");
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _httpClient.Dispose();
        }
    }

    /// <summary>
    /// Exception thrown for Dropbox API errors.
    /// </summary>
    public class DropboxApiException : Exception
    {
        /// <summary>
        /// Gets the Dropbox error tag.
        /// </summary>
        public string ErrorTag { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DropboxApiException"/> class.
        /// </summary>
        /// <param name="errorTag">The error tag from Dropbox.</param>
        /// <param name="message">The error message.</param>
        public DropboxApiException(string errorTag, string message) : base(message)
        {
            ErrorTag = errorTag;
        }
    }
}
