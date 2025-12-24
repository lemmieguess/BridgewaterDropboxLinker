using Bridgewater.DropboxLinker.Outlook.Services;
using FluentAssertions;
using Xunit;

namespace Bridgewater.DropboxLinker.Outlook.Tests
{
    /// <summary>
    /// Tests for the <see cref="LinkConversionTracker"/> class.
    /// </summary>
    public class LinkConversionTrackerTests
    {
        private readonly LinkConversionTracker _sut = new LinkConversionTracker();

        [Fact]
        public void AddConversion_AddsNewConversion()
        {
            // Arrange
            var emailId = "email-123";
            var conversion = new LinkConversionState
            {
                FileName = "test.pdf",
                LocalPath = @"C:\Dropbox\test.pdf",
                Status = ConversionStatus.Pending
            };

            // Act
            _sut.AddConversion(emailId, conversion);

            // Assert
            var result = _sut.GetConversions(emailId);
            result.Should().HaveCount(1);
            result![0].FileName.Should().Be("test.pdf");
        }

        [Fact]
        public void AddConversion_AddsMultipleConversions()
        {
            // Arrange
            var emailId = "email-123";
            var conversion1 = new LinkConversionState
            {
                FileName = "test1.pdf",
                LocalPath = @"C:\Dropbox\test1.pdf",
                Status = ConversionStatus.Pending
            };
            var conversion2 = new LinkConversionState
            {
                FileName = "test2.pdf",
                LocalPath = @"C:\Dropbox\test2.pdf",
                Status = ConversionStatus.InProgress
            };

            // Act
            _sut.AddConversion(emailId, conversion1);
            _sut.AddConversion(emailId, conversion2);

            // Assert
            var result = _sut.GetConversions(emailId);
            result.Should().HaveCount(2);
        }

        [Fact]
        public void GetConversions_ReturnsNullForUnknownEmail()
        {
            // Act
            var result = _sut.GetConversions("unknown-email");

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void GetFailedConversions_ReturnsOnlyFailed()
        {
            // Arrange
            var emailId = "email-123";
            _sut.AddConversion(emailId, new LinkConversionState
            {
                FileName = "success.pdf",
                LocalPath = @"C:\Dropbox\success.pdf",
                Status = ConversionStatus.Success
            });
            _sut.AddConversion(emailId, new LinkConversionState
            {
                FileName = "failed.pdf",
                LocalPath = @"C:\Dropbox\failed.pdf",
                Status = ConversionStatus.Failed
            });

            // Act
            var result = _sut.GetFailedConversions(emailId);

            // Assert
            result.Should().HaveCount(1);
            result[0].FileName.Should().Be("failed.pdf");
        }

        [Fact]
        public void GetFailedConversions_ReturnsEmptyForUnknownEmail()
        {
            // Act
            var result = _sut.GetFailedConversions("unknown-email");

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void UpdateConversion_UpdatesExistingConversion()
        {
            // Arrange
            var emailId = "email-123";
            var localPath = @"C:\Dropbox\test.pdf";
            _sut.AddConversion(emailId, new LinkConversionState
            {
                FileName = "test.pdf",
                LocalPath = localPath,
                Status = ConversionStatus.InProgress
            });

            // Act
            _sut.UpdateConversion(
                emailId,
                localPath,
                ConversionStatus.Success,
                resultUrl: "https://dropbox.com/s/abc/test.pdf");

            // Assert
            var result = _sut.GetConversions(emailId);
            result![0].Status.Should().Be(ConversionStatus.Success);
            result[0].ResultUrl.Should().Be("https://dropbox.com/s/abc/test.pdf");
        }

        [Fact]
        public void UpdateConversion_SetsErrorMessage()
        {
            // Arrange
            var emailId = "email-123";
            var localPath = @"C:\Dropbox\test.pdf";
            _sut.AddConversion(emailId, new LinkConversionState
            {
                FileName = "test.pdf",
                LocalPath = localPath,
                Status = ConversionStatus.InProgress
            });

            // Act
            _sut.UpdateConversion(
                emailId,
                localPath,
                ConversionStatus.Failed,
                errorMessage: "API rate limit exceeded");

            // Assert
            var result = _sut.GetConversions(emailId);
            result![0].Status.Should().Be(ConversionStatus.Failed);
            result[0].ErrorMessage.Should().Be("API rate limit exceeded");
        }

        [Fact]
        public void RemoveConversion_RemovesSpecificConversion()
        {
            // Arrange
            var emailId = "email-123";
            var localPath1 = @"C:\Dropbox\test1.pdf";
            var localPath2 = @"C:\Dropbox\test2.pdf";
            _sut.AddConversion(emailId, new LinkConversionState
            {
                FileName = "test1.pdf",
                LocalPath = localPath1,
                Status = ConversionStatus.Success
            });
            _sut.AddConversion(emailId, new LinkConversionState
            {
                FileName = "test2.pdf",
                LocalPath = localPath2,
                Status = ConversionStatus.Failed
            });

            // Act
            _sut.RemoveConversion(emailId, localPath1);

            // Assert
            var result = _sut.GetConversions(emailId);
            result.Should().HaveCount(1);
            result![0].FileName.Should().Be("test2.pdf");
        }

        [Fact]
        public void RemoveConversion_RemovesEmailWhenEmpty()
        {
            // Arrange
            var emailId = "email-123";
            var localPath = @"C:\Dropbox\test.pdf";
            _sut.AddConversion(emailId, new LinkConversionState
            {
                FileName = "test.pdf",
                LocalPath = localPath,
                Status = ConversionStatus.Success
            });

            // Act
            _sut.RemoveConversion(emailId, localPath);

            // Assert
            _sut.GetConversions(emailId).Should().BeNull();
        }

        [Fact]
        public void ClearConversions_RemovesAllConversions()
        {
            // Arrange
            var emailId = "email-123";
            _sut.AddConversion(emailId, new LinkConversionState
            {
                FileName = "test1.pdf",
                LocalPath = @"C:\Dropbox\test1.pdf",
                Status = ConversionStatus.Success
            });
            _sut.AddConversion(emailId, new LinkConversionState
            {
                FileName = "test2.pdf",
                LocalPath = @"C:\Dropbox\test2.pdf",
                Status = ConversionStatus.Success
            });

            // Act
            _sut.ClearConversions(emailId);

            // Assert
            _sut.GetConversions(emailId).Should().BeNull();
        }

        [Fact]
        public void HasFailedConversions_ReturnsTrueWhenFailed()
        {
            // Arrange
            var emailId = "email-123";
            _sut.AddConversion(emailId, new LinkConversionState
            {
                FileName = "test.pdf",
                LocalPath = @"C:\Dropbox\test.pdf",
                Status = ConversionStatus.Failed
            });

            // Act & Assert
            _sut.HasFailedConversions(emailId).Should().BeTrue();
        }

        [Fact]
        public void HasFailedConversions_ReturnsFalseWhenNoFailed()
        {
            // Arrange
            var emailId = "email-123";
            _sut.AddConversion(emailId, new LinkConversionState
            {
                FileName = "test.pdf",
                LocalPath = @"C:\Dropbox\test.pdf",
                Status = ConversionStatus.Success
            });

            // Act & Assert
            _sut.HasFailedConversions(emailId).Should().BeFalse();
        }

        [Fact]
        public void HasFailedConversions_ReturnsFalseForUnknownEmail()
        {
            // Act & Assert
            _sut.HasFailedConversions("unknown-email").Should().BeFalse();
        }

        [Fact]
        public void HasPendingConversions_ReturnsTrueForPending()
        {
            // Arrange
            var emailId = "email-123";
            _sut.AddConversion(emailId, new LinkConversionState
            {
                FileName = "test.pdf",
                LocalPath = @"C:\Dropbox\test.pdf",
                Status = ConversionStatus.Pending
            });

            // Act & Assert
            _sut.HasPendingConversions(emailId).Should().BeTrue();
        }

        [Fact]
        public void HasPendingConversions_ReturnsTrueForInProgress()
        {
            // Arrange
            var emailId = "email-123";
            _sut.AddConversion(emailId, new LinkConversionState
            {
                FileName = "test.pdf",
                LocalPath = @"C:\Dropbox\test.pdf",
                Status = ConversionStatus.InProgress
            });

            // Act & Assert
            _sut.HasPendingConversions(emailId).Should().BeTrue();
        }

        [Fact]
        public void HasPendingConversions_ReturnsFalseForCompleted()
        {
            // Arrange
            var emailId = "email-123";
            _sut.AddConversion(emailId, new LinkConversionState
            {
                FileName = "test.pdf",
                LocalPath = @"C:\Dropbox\test.pdf",
                Status = ConversionStatus.Success
            });

            // Act & Assert
            _sut.HasPendingConversions(emailId).Should().BeFalse();
        }

        [Fact]
        public void TracksDifferentEmailsSeparately()
        {
            // Arrange
            var emailId1 = "email-1";
            var emailId2 = "email-2";
            _sut.AddConversion(emailId1, new LinkConversionState
            {
                FileName = "file1.pdf",
                LocalPath = @"C:\Dropbox\file1.pdf",
                Status = ConversionStatus.Success
            });
            _sut.AddConversion(emailId2, new LinkConversionState
            {
                FileName = "file2.pdf",
                LocalPath = @"C:\Dropbox\file2.pdf",
                Status = ConversionStatus.Failed
            });

            // Assert
            _sut.HasFailedConversions(emailId1).Should().BeFalse();
            _sut.HasFailedConversions(emailId2).Should().BeTrue();
            _sut.GetConversions(emailId1)![0].FileName.Should().Be("file1.pdf");
            _sut.GetConversions(emailId2)![0].FileName.Should().Be("file2.pdf");
        }
    }
}
