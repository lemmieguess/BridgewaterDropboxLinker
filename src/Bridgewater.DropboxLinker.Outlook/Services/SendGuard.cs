using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Office.Interop.Outlook;

namespace Bridgewater.DropboxLinker.Outlook.Services
{
    /// <summary>
    /// Validates email messages before sending to enforce Dropbox link policies.
    /// </summary>
    public sealed class SendGuard
    {
        /// <summary>
        /// Default threshold for large attachment warnings (10 MB).
        /// </summary>
        public const long DefaultLargeAttachmentThresholdBytes = 10 * 1024 * 1024;

        /// <summary>
        /// Validates a mail item before sending.
        /// </summary>
        /// <param name="mailItem">The Outlook mail item to validate.</param>
        /// <param name="conversionStates">Current state of any link conversions for this message.</param>
        /// <param name="thresholdBytes">Size threshold for large attachment warnings.</param>
        /// <returns>The validation result indicating whether to block or warn.</returns>
        public SendValidationResult Validate(
            object mailItem,
            IReadOnlyList<LinkConversionState>? conversionStates = null,
            long thresholdBytes = DefaultLargeAttachmentThresholdBytes)
        {
            var failedConversions = new List<LinkConversionState>();
            var largeAttachments = new List<LargeAttachmentInfo>();

            // Check for failed conversions first (highest priority - blocks send)
            if (conversionStates != null)
            {
                failedConversions = conversionStates
                    .Where(s => s.Status == ConversionStatus.Failed)
                    .ToList();

                if (failedConversions.Count > 0)
                {
                    var fileNames = string.Join(", ", failedConversions.Select(f => $"'{f.FileName}'"));
                    return new SendValidationResult
                    {
                        BlockSend = true,
                        ShowLargeAttachmentWarning = false,
                        Message = failedConversions.Count == 1
                            ? $"Cannot send: Dropbox link creation failed for {fileNames}. " +
                              "Please retry, re-authenticate, or remove the failed block."
                            : $"Cannot send: Dropbox link creation failed for {failedConversions.Count} files: {fileNames}. " +
                              "Please resolve all issues before sending.",
                        FailedConversions = failedConversions
                    };
                }

                // Check for pending/in-progress conversions (also blocks send)
                var pendingConversions = conversionStates
                    .Where(s => s.Status == ConversionStatus.Pending || s.Status == ConversionStatus.InProgress)
                    .ToList();

                if (pendingConversions.Count > 0)
                {
                    return new SendValidationResult
                    {
                        BlockSend = true,
                        ShowLargeAttachmentWarning = false,
                        Message = "Please wait: Dropbox links are still being created.",
                        FailedConversions = null
                    };
                }
            }

            // Check attachments for size (shows warning but doesn't block)
            if (mailItem is MailItem outlook)
            {
                try
                {
                    var attachments = outlook.Attachments;
                    for (int i = 1; i <= attachments.Count; i++)
                    {
                        var attachment = attachments[i];
                        try
                        {
                            // Get attachment size
                            long sizeBytes = attachment.Size;

                            if (sizeBytes >= thresholdBytes)
                            {
                                largeAttachments.Add(new LargeAttachmentInfo
                                {
                                    FileName = attachment.FileName ?? $"Attachment {i}",
                                    SizeBytes = sizeBytes
                                });
                            }
                        }
                        finally
                        {
                            // Release COM object
                            System.Runtime.InteropServices.Marshal.ReleaseComObject(attachment);
                        }
                    }
                }
                catch
                {
                    // If we can't read attachments, don't block send
                    // Log this error in calling code if needed
                }
            }

            if (largeAttachments.Count > 0)
            {
                var totalSize = largeAttachments.Sum(a => a.SizeBytes);
                return new SendValidationResult
                {
                    BlockSend = false,
                    ShowLargeAttachmentWarning = true,
                    Message = largeAttachments.Count == 1
                        ? $"Large attachment detected: '{largeAttachments[0].FileName}' " +
                          $"({FormatBytes(largeAttachments[0].SizeBytes)}). " +
                          "Consider using a Dropbox link instead."
                        : $"{largeAttachments.Count} large attachments detected " +
                          $"(total: {FormatBytes(totalSize)}). " +
                          "Consider using Dropbox links instead.",
                    LargeAttachments = largeAttachments
                };
            }

            return new SendValidationResult
            {
                BlockSend = false,
                ShowLargeAttachmentWarning = false,
                Message = string.Empty
            };
        }

        /// <summary>
        /// Formats bytes into a human-readable string.
        /// </summary>
        private static string FormatBytes(long bytes)
        {
            const long KB = 1024;
            const long MB = KB * 1024;
            const long GB = MB * 1024;

            if (bytes >= GB)
                return $"{bytes / (double)GB:F1} GB";
            if (bytes >= MB)
                return $"{bytes / (double)MB:F1} MB";
            if (bytes >= KB)
                return $"{bytes / (double)KB:F1} KB";
            return $"{bytes} bytes";
        }
    }

    /// <summary>
    /// Result of send validation.
    /// </summary>
    public sealed class SendValidationResult
    {
        /// <summary>
        /// Gets or sets a value indicating whether the send should be blocked.
        /// </summary>
        public bool BlockSend { get; init; }

        /// <summary>
        /// Gets or sets a value indicating whether to show a large attachment warning.
        /// </summary>
        public bool ShowLargeAttachmentWarning { get; init; }

        /// <summary>
        /// Gets or sets the message to display to the user.
        /// </summary>
        public string Message { get; init; } = string.Empty;

        /// <summary>
        /// Gets or sets the list of failed conversions, if any.
        /// </summary>
        public IReadOnlyList<LinkConversionState>? FailedConversions { get; init; }

        /// <summary>
        /// Gets or sets the list of large attachments found, if any.
        /// </summary>
        public IReadOnlyList<LargeAttachmentInfo>? LargeAttachments { get; init; }
    }

    /// <summary>
    /// Tracks the state of a Dropbox link conversion.
    /// </summary>
    public sealed class LinkConversionState
    {
        /// <summary>
        /// Gets or sets the original file name.
        /// </summary>
        public string FileName { get; init; } = string.Empty;

        /// <summary>
        /// Gets or sets the local file path.
        /// </summary>
        public string LocalPath { get; init; } = string.Empty;

        /// <summary>
        /// Gets or sets the conversion status.
        /// </summary>
        public ConversionStatus Status { get; set; }

        /// <summary>
        /// Gets or sets the error message if the conversion failed.
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets the resulting URL if successful.
        /// </summary>
        public string? ResultUrl { get; set; }
    }

    /// <summary>
    /// Status of a link conversion operation.
    /// </summary>
    public enum ConversionStatus
    {
        /// <summary>Conversion is pending.</summary>
        Pending,

        /// <summary>Conversion is in progress.</summary>
        InProgress,

        /// <summary>Conversion completed successfully.</summary>
        Success,

        /// <summary>Conversion failed.</summary>
        Failed
    }

    /// <summary>
    /// Information about a large attachment.
    /// </summary>
    public sealed class LargeAttachmentInfo
    {
        /// <summary>
        /// Gets or sets the attachment file name.
        /// </summary>
        public string FileName { get; init; } = string.Empty;

        /// <summary>
        /// Gets or sets the attachment size in bytes.
        /// </summary>
        public long SizeBytes { get; init; }
    }
}
