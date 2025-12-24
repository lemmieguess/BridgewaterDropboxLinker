using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Bridgewater.DropboxLinker.Core.Utilities
{
    /// <summary>
    /// Cleans file names for display in email link blocks.
    /// </summary>
    public static class FileNameCleaner
    {
        // Pattern to match common version and draft tokens
        private static readonly Regex TokenPattern = new Regex(
            @"\b(v\d+|rev\d+|r\d+|draft|copy|final)\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // Pattern to collapse multiple whitespace characters
        private static readonly Regex WhitespacePattern = new Regex(
            @"\s+",
            RegexOptions.Compiled);

        /// <summary>
        /// Cleans a file name for display by removing version tokens and formatting nicely.
        /// </summary>
        /// <param name="fileName">The original file name.</param>
        /// <returns>A cleaned, title-cased file name suitable for display.</returns>
        /// <remarks>
        /// Cleaning rules:
        /// <list type="bullet">
        ///   <item>Replace '_' and '-' with spaces</item>
        ///   <item>Strip common version and draft tokens (v1, rev2, draft, copy, final)</item>
        ///   <item>Collapse multiple whitespace characters</item>
        ///   <item>Apply title case to the base name</item>
        ///   <item>Preserve the file extension</item>
        /// </list>
        /// </remarks>
        public static string CleanForDisplay(string? fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return fileName ?? string.Empty;
            }

            // Split base name and extension
            var lastDot = fileName.LastIndexOf('.');
            var baseName = lastDot > 0 ? fileName.Substring(0, lastDot) : fileName;
            var extension = lastDot > 0 ? fileName.Substring(lastDot) : string.Empty;

            // Replace separators with spaces
            baseName = baseName.Replace('_', ' ').Replace('-', ' ');

            // Strip version and draft tokens
            baseName = TokenPattern.Replace(baseName, " ");

            // Collapse whitespace
            baseName = WhitespacePattern.Replace(baseName, " ").Trim();

            // Apply title case
            var textInfo = CultureInfo.InvariantCulture.TextInfo;
            baseName = textInfo.ToTitleCase(baseName.ToLowerInvariant());

            return $"{baseName}{extension}";
        }
    }
}
