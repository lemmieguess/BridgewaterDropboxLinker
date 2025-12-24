using System;
using System.Threading;
using System.Threading.Tasks;

namespace Bridgewater.DropboxLinker.Core.Contracts
{
    /// <summary>
    /// Represents a request to create a shared Dropbox link.
    /// </summary>
    public sealed class LinkRequest
    {
        /// <summary>
        /// Gets the local file system path to the file.
        /// </summary>
        public string LocalFilePath { get; init; } = string.Empty;

        /// <summary>
        /// Gets the file size in bytes.
        /// </summary>
        public long FileSizeBytes { get; init; }

        /// <summary>
        /// Gets the UTC expiration time for the shared link.
        /// </summary>
        public DateTimeOffset ExpiresAtUtc { get; init; }
    }

    /// <summary>
    /// Represents the result of creating a shared Dropbox link.
    /// </summary>
    public sealed class LinkResult
    {
        /// <summary>
        /// Gets the shared link URL.
        /// </summary>
        public string Url { get; init; } = string.Empty;

        /// <summary>
        /// Gets the cleaned display name for the file.
        /// </summary>
        public string DisplayName { get; init; } = string.Empty;

        /// <summary>
        /// Gets the Dropbox API path for the file.
        /// </summary>
        public string DropboxPath { get; init; } = string.Empty;

        /// <summary>
        /// Gets a value indicating whether an existing shared link was reused.
        /// </summary>
        public bool ReusedExisting { get; init; }
    }

    /// <summary>
    /// Locates the Dropbox business folder on the local file system.
    /// </summary>
    public interface IDropboxFolderLocator
    {
        /// <summary>
        /// Gets the local path to the Dropbox business folder.
        /// </summary>
        /// <returns>The absolute path to the Dropbox business folder.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the Dropbox folder cannot be found.
        /// </exception>
        string GetBusinessDropboxRoot();
    }

    /// <summary>
    /// Manages Dropbox OAuth authentication.
    /// </summary>
    public interface IDropboxAuthService
    {
        /// <summary>
        /// Gets an authenticated Dropbox client.
        /// </summary>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>An authenticated Dropbox client instance.</returns>
        /// <remarks>
        /// Returns object type as placeholder. Replace with DropboxClient once
        /// the Dropbox .NET SDK is added to the project.
        /// </remarks>
        Task<object> GetClientAsync(CancellationToken ct);

        /// <summary>
        /// Forces re-authentication with Dropbox.
        /// </summary>
        /// <param name="ct">Cancellation token.</param>
        Task ReauthenticateAsync(CancellationToken ct);
    }

    /// <summary>
    /// Maps local file paths to Dropbox API paths.
    /// </summary>
    public interface IDropboxPathMapper
    {
        /// <summary>
        /// Converts a local file path to a Dropbox API path.
        /// </summary>
        /// <param name="dropboxRootLocalPath">The local path to the Dropbox root folder.</param>
        /// <param name="localPath">The local path to convert.</param>
        /// <returns>The Dropbox API path (starting with "/").</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the local path is not inside the Dropbox root.
        /// </exception>
        string ToDropboxPath(string dropboxRootLocalPath, string localPath);
    }

    /// <summary>
    /// Creates and manages Dropbox shared links.
    /// </summary>
    public interface IDropboxLinkService
    {
        /// <summary>
        /// Creates a new shared link or retrieves an existing one.
        /// </summary>
        /// <param name="req">The link request details.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>The result containing the shared link URL and metadata.</returns>
        Task<LinkResult> CreateOrReuseSharedLinkAsync(LinkRequest req, CancellationToken ct);
    }

    /// <summary>
    /// Builds HTML and plain text representations of link blocks.
    /// </summary>
    public interface ILinkBlockBuilder
    {
        /// <summary>
        /// Builds an HTML block for insertion into an email.
        /// </summary>
        /// <param name="link">The link result containing URL and display name.</param>
        /// <param name="fileSizeBytes">The file size in bytes.</param>
        /// <returns>An HTML string representing the link block.</returns>
        string BuildHtmlBlock(LinkResult link, long fileSizeBytes);

        /// <summary>
        /// Builds a plain text block for insertion into an email.
        /// </summary>
        /// <param name="link">The link result containing URL and display name.</param>
        /// <param name="fileSizeBytes">The file size in bytes.</param>
        /// <returns>A plain text string representing the link block.</returns>
        string BuildPlainTextBlock(LinkResult link, long fileSizeBytes);
    }

    /// <summary>
    /// Application logging interface.
    /// </summary>
    public interface IAppLogger
    {
        /// <summary>
        /// Logs an informational message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        void Info(string message);

        /// <summary>
        /// Logs a warning message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        void Warn(string message);

        /// <summary>
        /// Logs an error with an exception.
        /// </summary>
        /// <param name="ex">The exception that occurred.</param>
        /// <param name="message">Additional context message.</param>
        void Error(Exception ex, string message);
    }
}
