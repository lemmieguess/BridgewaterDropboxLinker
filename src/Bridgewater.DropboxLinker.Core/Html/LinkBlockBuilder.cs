using System;
using Bridgewater.DropboxLinker.Core.Contracts;
using Bridgewater.DropboxLinker.Core.Utilities;

namespace Bridgewater.DropboxLinker.Core.Html
{
    /// <summary>
    /// Builds HTML and plain text link blocks for insertion into emails.
    /// </summary>
    /// <remarks>
    /// The HTML block uses:
    /// <list type="bullet">
    ///   <item>Table-based layout for maximum email client compatibility</item>
    ///   <item>Inline styles (no external CSS dependencies)</item>
    ///   <item>No remote images</item>
    ///   <item>Neutral, professional styling</item>
    /// </list>
    /// </remarks>
    public sealed class LinkBlockBuilder : ILinkBlockBuilder
    {
        private const string BlockLabel = "Dropbox link";
        private const string ActionText = "Open";

        /// <inheritdoc />
        public string BuildHtmlBlock(LinkResult link, long fileSizeBytes)
        {
            if (link == null)
            {
                throw new ArgumentNullException(nameof(link));
            }

            var size = ByteSizeFormatter.ToHumanReadable(fileSizeBytes);
            var name = link.DisplayName ?? string.Empty;
            var url = link.Url ?? string.Empty;

            // Table-based layout with inline styles for maximum email client compatibility
            return $@"<table role=""presentation"" cellpadding=""0"" cellspacing=""0"" width=""100%"" style=""max-width:520px;border:1px solid #d9d9d9;border-radius:8px;"">
  <tr>
    <td style=""padding:12px 14px;font-family:Arial, Helvetica, sans-serif;"">
      <div style=""font-size:12px;letter-spacing:0.4px;color:#6b6b6b;text-transform:uppercase;"">{BlockLabel}</div>
      <div style=""font-size:16px;color:#111;margin-top:6px;line-height:1.2;""><strong>{EscapeHtml(name)}</strong></div>
      <div style=""font-size:12px;color:#6b6b6b;margin-top:4px;"">{EscapeHtml(size)}</div>
      <div style=""margin-top:10px;"">
        <a href=""{EscapeHtmlAttribute(url)}"" style=""display:inline-block;text-decoration:none;padding:8px 12px;border:1px solid #111;border-radius:6px;color:#111;font-size:13px;"">{ActionText}</a>
      </div>
    </td>
  </tr>
</table>";
        }

        /// <inheritdoc />
        public string BuildPlainTextBlock(LinkResult link, long fileSizeBytes)
        {
            if (link == null)
            {
                throw new ArgumentNullException(nameof(link));
            }

            var size = ByteSizeFormatter.ToHumanReadable(fileSizeBytes);
            var name = link.DisplayName ?? string.Empty;
            var url = link.Url ?? string.Empty;

            return $"{BlockLabel}\r\n{name} ({size})\r\n{ActionText}: {url}\r\n";
        }

        /// <summary>
        /// Escapes a string for safe inclusion in HTML content.
        /// </summary>
        private static string EscapeHtml(string? input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }

            return input
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;");
        }

        /// <summary>
        /// Escapes a string for safe inclusion in an HTML attribute value.
        /// </summary>
        private static string EscapeHtmlAttribute(string? input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }

            return EscapeHtml(input).Replace("\"", "&quot;");
        }
    }
}
