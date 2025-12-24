using System;
using System.IO;
using System.Text.Json;
using Bridgewater.DropboxLinker.Core.Contracts;

namespace Bridgewater.DropboxLinker.Core.Dropbox
{
    /// <summary>
    /// Locates the Dropbox business folder using the Dropbox desktop app configuration.
    /// </summary>
    /// <remarks>
    /// The Dropbox desktop app stores its configuration in info.json, typically located at:
    /// <list type="bullet">
    ///   <item>%APPDATA%\Dropbox\info.json</item>
    ///   <item>%LOCALAPPDATA%\Dropbox\info.json</item>
    /// </list>
    /// </remarks>
    public sealed class DropboxFolderLocator : IDropboxFolderLocator
    {
        private const string DropboxConfigFileName = "info.json";
        private const string DropboxFolderName = "Dropbox";
        private const string BusinessPropertyName = "business";
        private const string PathPropertyName = "path";

        /// <inheritdoc />
        public string GetBusinessDropboxRoot()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            var candidatePaths = new[]
            {
                Path.Combine(appData, DropboxFolderName, DropboxConfigFileName),
                Path.Combine(localAppData, DropboxFolderName, DropboxConfigFileName)
            };

            foreach (var configPath in candidatePaths)
            {
                var businessPath = TryGetBusinessPathFromConfig(configPath);
                if (!string.IsNullOrEmpty(businessPath))
                {
                    return businessPath;
                }
            }

            throw new InvalidOperationException(
                "Dropbox business folder not found. " +
                "Ensure the Dropbox desktop app is installed and you are signed in to a business account.");
        }

        private static string? TryGetBusinessPathFromConfig(string configPath)
        {
            if (!File.Exists(configPath))
            {
                return null;
            }

            try
            {
                using var stream = File.OpenRead(configPath);
                using var doc = JsonDocument.Parse(stream);

                if (!doc.RootElement.TryGetProperty(BusinessPropertyName, out var business))
                {
                    return null;
                }

                if (!business.TryGetProperty(PathPropertyName, out var pathProp))
                {
                    return null;
                }

                var path = pathProp.GetString();
                if (string.IsNullOrWhiteSpace(path))
                {
                    return null;
                }

                if (!Directory.Exists(path))
                {
                    return null;
                }

                return path;
            }
            catch (JsonException)
            {
                // Config file is malformed; try next candidate
                return null;
            }
            catch (IOException)
            {
                // File access issue; try next candidate
                return null;
            }
        }
    }
}
