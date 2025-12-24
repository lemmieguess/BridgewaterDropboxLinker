using System;
using System.IO;
using Bridgewater.DropboxLinker.Core.Contracts;

namespace Bridgewater.DropboxLinker.Core.Dropbox
{
    /// <summary>
    /// Maps local file system paths to Dropbox API paths.
    /// </summary>
    public sealed class DropboxPathMapper : IDropboxPathMapper
    {
        /// <inheritdoc />
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="dropboxRootLocalPath"/> or <paramref name="localPath"/> is null or whitespace.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when <paramref name="localPath"/> is not inside the Dropbox root folder.
        /// </exception>
        public string ToDropboxPath(string dropboxRootLocalPath, string localPath)
        {
            if (string.IsNullOrWhiteSpace(dropboxRootLocalPath))
            {
                throw new ArgumentException("Dropbox root path is required.", nameof(dropboxRootLocalPath));
            }

            if (string.IsNullOrWhiteSpace(localPath))
            {
                throw new ArgumentException("Local path is required.", nameof(localPath));
            }

            // Normalize paths and ensure root ends with separator for accurate prefix matching
            var normalizedRoot = Path.GetFullPath(dropboxRootLocalPath)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;

            var normalizedLocal = Path.GetFullPath(localPath);

            if (!normalizedLocal.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"The file is not inside the Dropbox folder. " +
                    $"Expected path under: {normalizedRoot}");
            }

            // Extract the relative path and convert to Dropbox format
            var relativePath = normalizedLocal.Substring(normalizedRoot.Length);
            var dropboxPath = relativePath
                .Replace(Path.DirectorySeparatorChar, '/')
                .Replace(Path.AltDirectorySeparatorChar, '/');

            return "/" + dropboxPath;
        }

        /// <summary>
        /// Validates whether a local path is inside the Dropbox root folder.
        /// </summary>
        /// <param name="dropboxRootLocalPath">The local path to the Dropbox root folder.</param>
        /// <param name="localPath">The local path to validate.</param>
        /// <returns><c>true</c> if the path is inside the Dropbox root; otherwise, <c>false</c>.</returns>
        public bool IsInsideDropboxRoot(string dropboxRootLocalPath, string localPath)
        {
            if (string.IsNullOrWhiteSpace(dropboxRootLocalPath) || string.IsNullOrWhiteSpace(localPath))
            {
                return false;
            }

            try
            {
                var normalizedRoot = Path.GetFullPath(dropboxRootLocalPath)
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                    + Path.DirectorySeparatorChar;

                var normalizedLocal = Path.GetFullPath(localPath);

                return normalizedLocal.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }
    }
}
