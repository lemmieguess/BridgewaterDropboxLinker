using System;

namespace Bridgewater.DropboxLinker.Core.Utilities
{
    /// <summary>
    /// Provides human-readable formatting for byte sizes.
    /// </summary>
    public static class ByteSizeFormatter
    {
        private const double KB = 1024;
        private const double MB = KB * 1024;
        private const double GB = MB * 1024;

        /// <summary>
        /// Converts a byte count to a human-readable string (e.g., "1.5 MB").
        /// </summary>
        /// <param name="bytes">The number of bytes.</param>
        /// <returns>A formatted string with appropriate unit.</returns>
        public static string ToHumanReadable(long bytes)
        {
            if (bytes < 0)
            {
                return string.Empty;
            }

            if (bytes >= GB)
            {
                return $"{bytes / GB:0.0} GB";
            }

            if (bytes >= MB)
            {
                return $"{bytes / MB:0.0} MB";
            }

            if (bytes >= KB)
            {
                return $"{bytes / KB:0.0} KB";
            }

            return $"{bytes} B";
        }
    }
}
