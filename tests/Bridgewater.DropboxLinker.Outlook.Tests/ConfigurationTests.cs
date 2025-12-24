using FluentAssertions;
using Xunit;

namespace Bridgewater.DropboxLinker.Outlook.Tests
{
    /// <summary>
    /// Tests for the <see cref="Configuration"/> class.
    /// </summary>
    public class ConfigurationTests
    {
        [Fact]
        public void DefaultConfiguration_HasCorrectDefaults()
        {
            // Arrange
            var config = new Configuration();

            // Assert
            config.DropboxAppKey.Should().BeEmpty();
            config.RootNamespaceId.Should().BeNull();
            config.LargeAttachmentThresholdBytes.Should().Be(10 * 1024 * 1024);
            config.LinkExpirationDays.Should().Be(7);
            config.DebugLogging.Should().BeFalse();
        }

        [Fact]
        public void IsValid_ReturnsFalseForEmptyAppKey()
        {
            // Arrange
            var config = new Configuration
            {
                DropboxAppKey = ""
            };

            // Act & Assert
            config.IsValid().Should().BeFalse();
        }

        [Fact]
        public void IsValid_ReturnsFalseForWhitespaceAppKey()
        {
            // Arrange
            var config = new Configuration
            {
                DropboxAppKey = "   "
            };

            // Act & Assert
            config.IsValid().Should().BeFalse();
        }

        [Fact]
        public void IsValid_ReturnsTrueForValidAppKey()
        {
            // Arrange
            var config = new Configuration
            {
                DropboxAppKey = "abc123xyz"
            };

            // Act & Assert
            config.IsValid().Should().BeTrue();
        }

        [Fact]
        public void LargeAttachmentThreshold_DefaultIs10MB()
        {
            // Arrange
            var config = new Configuration();

            // Assert
            config.LargeAttachmentThresholdBytes.Should().Be(10 * 1024 * 1024);
        }

        [Fact]
        public void LinkExpirationDays_DefaultIs7()
        {
            // Arrange
            var config = new Configuration();

            // Assert
            config.LinkExpirationDays.Should().Be(7);
        }

        [Fact]
        public void CanSetCustomValues()
        {
            // Arrange
            var config = new Configuration
            {
                DropboxAppKey = "my-app-key",
                RootNamespaceId = "ns-12345",
                LargeAttachmentThresholdBytes = 20 * 1024 * 1024,
                LinkExpirationDays = 14,
                DebugLogging = true
            };

            // Assert
            config.DropboxAppKey.Should().Be("my-app-key");
            config.RootNamespaceId.Should().Be("ns-12345");
            config.LargeAttachmentThresholdBytes.Should().Be(20 * 1024 * 1024);
            config.LinkExpirationDays.Should().Be(14);
            config.DebugLogging.Should().BeTrue();
        }
    }
}
