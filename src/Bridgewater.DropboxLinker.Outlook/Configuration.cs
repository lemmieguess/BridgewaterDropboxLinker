using System;
using System.IO;
using System.Text.Json;

namespace Bridgewater.DropboxLinker.Outlook
{
    /// <summary>
    /// Application configuration settings.
    /// </summary>
    public sealed class Configuration
    {
        /// <summary>
        /// Gets or sets the Dropbox App Key.
        /// </summary>
        public string DropboxAppKey { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the optional Team Space root namespace ID.
        /// </summary>
        public string? RootNamespaceId { get; set; }

        /// <summary>
        /// Gets or sets the large attachment threshold in bytes.
        /// </summary>
        public long LargeAttachmentThresholdBytes { get; set; } = 10 * 1024 * 1024; // 10 MB

        /// <summary>
        /// Gets or sets the default link expiration in days.
        /// </summary>
        public int LinkExpirationDays { get; set; } = 7;

        /// <summary>
        /// Gets or sets whether debug logging is enabled.
        /// </summary>
        public bool DebugLogging { get; set; }

        /// <summary>
        /// Gets the path to the configuration file.
        /// </summary>
        private static string ConfigFilePath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Bridgewater", "DropboxLinker", "settings.json");

        /// <summary>
        /// Loads configuration from the settings file.
        /// </summary>
        /// <returns>The loaded configuration, or defaults if file doesn't exist.</returns>
        public static Configuration Load()
        {
            try
            {
                if (File.Exists(ConfigFilePath))
                {
                    var json = File.ReadAllText(ConfigFilePath);
                    var config = JsonSerializer.Deserialize<Configuration>(json);
                    
                    if (config != null && !string.IsNullOrEmpty(config.DropboxAppKey))
                    {
                        return config;
                    }
                }
            }
            catch
            {
                // Fall through to use defaults or prompt for configuration
            }

            // Check for environment variable override (useful for development)
            var envAppKey = Environment.GetEnvironmentVariable("DROPBOX_APP_KEY");
            var envNamespaceId = Environment.GetEnvironmentVariable("DROPBOX_ROOT_NAMESPACE_ID");

            if (!string.IsNullOrEmpty(envAppKey))
            {
                return new Configuration
                {
                    DropboxAppKey = envAppKey,
                    RootNamespaceId = envNamespaceId
                };
            }

            // Return defaults - the app will need to prompt for configuration
            return new Configuration
            {
                DropboxAppKey = "" // Will need to be configured
            };
        }

        /// <summary>
        /// Saves the configuration to the settings file.
        /// </summary>
        public void Save()
        {
            var directory = Path.GetDirectoryName(ConfigFilePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(this, options);
            File.WriteAllText(ConfigFilePath, json);
        }

        /// <summary>
        /// Validates the configuration.
        /// </summary>
        /// <returns><c>true</c> if the configuration is valid; otherwise, <c>false</c>.</returns>
        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(DropboxAppKey);
        }
    }
}
