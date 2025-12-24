using System;
using System.Collections.Generic;

namespace Bridgewater.DropboxLinker.Outlook.Services
{
    /// <summary>
    /// Validates email messages before sending to enforce Dropbox link policies.
    /// </summary>
    /// <remarks>
    /// This is a stub implementation. The full implementation should use
    /// Microsoft.Office.Interop.Outlook.MailItem to inspect attachments.
    /// </remarks>
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
        /// <remarks>
        /// TODO: Implement using Microsoft.Office.Interop.Outlook.MailItem
        /// - Inspect Attachments collection
        /// - Check for any attachment >= threshold
        /// - Check conversion state for failures
        /// </remarks>
        public SendValidationResult Validate(
            object mailItem,
            IReadOnlyList<LinkConversionState>? conversionStates = null,
            long thresholdBytes = DefaultLargeAttachmentThresholdBytes)
        {
            // Check for failed conversions first (highest priority)
            if (conversionStates != null)
            {
                foreach (var state in conversionStates)
                {
                    if (state.Status == ConversionStatus.Failed)
                    {
                        return new SendValidationResult
                        {
                            BlockSend = true,
                            ShowLargeAttachmentWarning = false,
                            Message = $"Cannot send: Dropbox link creation failed for '{state.FileName}'. " +
                                     "Please retry, re-authenticate, or remove the failed block.",
                            FailedConversions = new[] { state }
                        };
                    }
                }
            }

            // TODO: Check attachments for size
            // var outlook = (Microsoft.Office.Interop.Outlook.MailItem)mailItem;
            // foreach (var attachment in outlook.Attachments) { ... }

            return new SendValidationResult
            {
                BlockSend = false,
                ShowLargeAttachmentWarning = false,
                Message = string.Empty
            };
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
