using System.Collections.Generic;
using Bridgewater.DropboxLinker.Outlook.Services;
using FluentAssertions;
using Xunit;

namespace Bridgewater.DropboxLinker.Outlook.Tests
{
    /// <summary>
    /// Tests for the <see cref="SendGuard"/> class.
    /// </summary>
    /// <remarks>
    /// These tests focus on the conversion state validation logic.
    /// Attachment size checking requires Outlook COM and is tested manually.
    /// </remarks>
    public class SendGuardTests
    {
        private readonly SendGuard _sut = new SendGuard();

        [Fact]
        public void Validate_WithNoConversions_AllowsSend()
        {
            // Arrange
            object? mailItem = null;

            // Act
            var result = _sut.Validate(mailItem!, null);

            // Assert
            result.BlockSend.Should().BeFalse();
            result.ShowLargeAttachmentWarning.Should().BeFalse();
            result.Message.Should().BeEmpty();
        }

        [Fact]
        public void Validate_WithEmptyConversions_AllowsSend()
        {
            // Arrange
            object? mailItem = null;
            var conversions = new List<LinkConversionState>();

            // Act
            var result = _sut.Validate(mailItem!, conversions);

            // Assert
            result.BlockSend.Should().BeFalse();
            result.ShowLargeAttachmentWarning.Should().BeFalse();
        }

        [Fact]
        public void Validate_WithSuccessfulConversions_AllowsSend()
        {
            // Arrange
            object? mailItem = null;
            var conversions = new List<LinkConversionState>
            {
                new LinkConversionState
                {
                    FileName = "report.pdf",
                    LocalPath = @"C:\Dropbox\report.pdf",
                    Status = ConversionStatus.Success,
                    ResultUrl = "https://www.dropbox.com/s/abc123/report.pdf"
                }
            };

            // Act
            var result = _sut.Validate(mailItem!, conversions);

            // Assert
            result.BlockSend.Should().BeFalse();
            result.ShowLargeAttachmentWarning.Should().BeFalse();
        }

        [Fact]
        public void Validate_WithFailedConversion_BlocksSend()
        {
            // Arrange
            object? mailItem = null;
            var conversions = new List<LinkConversionState>
            {
                new LinkConversionState
                {
                    FileName = "report.pdf",
                    LocalPath = @"C:\Dropbox\report.pdf",
                    Status = ConversionStatus.Failed,
                    ErrorMessage = "API error"
                }
            };

            // Act
            var result = _sut.Validate(mailItem!, conversions);

            // Assert
            result.BlockSend.Should().BeTrue();
            result.Message.Should().Contain("report.pdf");
            result.Message.Should().Contain("failed");
            result.FailedConversions.Should().HaveCount(1);
        }

        [Fact]
        public void Validate_WithMultipleFailedConversions_BlocksSend()
        {
            // Arrange
            object? mailItem = null;
            var conversions = new List<LinkConversionState>
            {
                new LinkConversionState
                {
                    FileName = "report1.pdf",
                    LocalPath = @"C:\Dropbox\report1.pdf",
                    Status = ConversionStatus.Failed,
                    ErrorMessage = "API error"
                },
                new LinkConversionState
                {
                    FileName = "report2.pdf",
                    LocalPath = @"C:\Dropbox\report2.pdf",
                    Status = ConversionStatus.Failed,
                    ErrorMessage = "API error"
                }
            };

            // Act
            var result = _sut.Validate(mailItem!, conversions);

            // Assert
            result.BlockSend.Should().BeTrue();
            result.Message.Should().Contain("2 files");
            result.FailedConversions.Should().HaveCount(2);
        }

        [Fact]
        public void Validate_WithPendingConversion_BlocksSend()
        {
            // Arrange
            object? mailItem = null;
            var conversions = new List<LinkConversionState>
            {
                new LinkConversionState
                {
                    FileName = "report.pdf",
                    LocalPath = @"C:\Dropbox\report.pdf",
                    Status = ConversionStatus.Pending
                }
            };

            // Act
            var result = _sut.Validate(mailItem!, conversions);

            // Assert
            result.BlockSend.Should().BeTrue();
            result.Message.Should().Contain("still being created");
        }

        [Fact]
        public void Validate_WithInProgressConversion_BlocksSend()
        {
            // Arrange
            object? mailItem = null;
            var conversions = new List<LinkConversionState>
            {
                new LinkConversionState
                {
                    FileName = "report.pdf",
                    LocalPath = @"C:\Dropbox\report.pdf",
                    Status = ConversionStatus.InProgress
                }
            };

            // Act
            var result = _sut.Validate(mailItem!, conversions);

            // Assert
            result.BlockSend.Should().BeTrue();
            result.Message.Should().Contain("still being created");
        }

        [Fact]
        public void Validate_WithMixedConversions_FailedTakesPriority()
        {
            // Arrange
            object? mailItem = null;
            var conversions = new List<LinkConversionState>
            {
                new LinkConversionState
                {
                    FileName = "success.pdf",
                    LocalPath = @"C:\Dropbox\success.pdf",
                    Status = ConversionStatus.Success,
                    ResultUrl = "https://www.dropbox.com/s/abc/success.pdf"
                },
                new LinkConversionState
                {
                    FileName = "failed.pdf",
                    LocalPath = @"C:\Dropbox\failed.pdf",
                    Status = ConversionStatus.Failed,
                    ErrorMessage = "API error"
                }
            };

            // Act
            var result = _sut.Validate(mailItem!, conversions);

            // Assert
            result.BlockSend.Should().BeTrue();
            result.Message.Should().Contain("failed.pdf");
            result.FailedConversions.Should().HaveCount(1);
            result.FailedConversions![0].FileName.Should().Be("failed.pdf");
        }

        [Fact]
        public void DefaultThreshold_Is10MB()
        {
            // Assert
            SendGuard.DefaultLargeAttachmentThresholdBytes.Should().Be(10 * 1024 * 1024);
        }
    }
}
