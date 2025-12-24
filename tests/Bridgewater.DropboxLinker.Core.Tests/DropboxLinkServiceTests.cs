using System;
using System.Threading;
using System.Threading.Tasks;
using Bridgewater.DropboxLinker.Core.Contracts;
using Bridgewater.DropboxLinker.Core.Dropbox;
using FluentAssertions;
using Xunit;

namespace Bridgewater.DropboxLinker.Core.Tests
{
    public class DropboxLinkServiceTests
    {
        [Fact]
        public async Task CreateOrReuseSharedLinkAsync_ThrowsOnNullRequest()
        {
            // Arrange
            var authService = new MockAuthService();
            var folderLocator = new MockFolderLocator();
            var pathMapper = new DropboxPathMapper();
            var logger = new MockLogger();
            
            var service = new DropboxLinkService(
                authService, folderLocator, pathMapper, logger);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => service.CreateOrReuseSharedLinkAsync(null!, CancellationToken.None));
        }

        [Fact]
        public async Task CreateOrReuseSharedLinkAsync_ThrowsOnEmptyLocalPath()
        {
            // Arrange
            var authService = new MockAuthService();
            var folderLocator = new MockFolderLocator();
            var pathMapper = new DropboxPathMapper();
            var logger = new MockLogger();
            
            var service = new DropboxLinkService(
                authService, folderLocator, pathMapper, logger);

            var request = new LinkRequest
            {
                LocalFilePath = "",
                FileSizeBytes = 1024,
                ExpiresAtUtc = DateTimeOffset.UtcNow.AddDays(7)
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => service.CreateOrReuseSharedLinkAsync(request, CancellationToken.None));
        }

        [Fact]
        public void DropboxApiException_StoresErrorTag()
        {
            // Arrange & Act
            var exception = new DropboxApiException("shared_link_already_exists", "Test message");

            // Assert
            exception.ErrorTag.Should().Be("shared_link_already_exists");
            exception.Message.Should().Be("Test message");
        }

        /// <summary>
        /// Mock auth service for testing.
        /// </summary>
        private sealed class MockAuthService : IDropboxAuthService
        {
            public Task<object> GetClientAsync(CancellationToken ct)
            {
                return Task.FromResult<object>("mock_access_token");
            }

            public Task ReauthenticateAsync(CancellationToken ct)
            {
                return Task.CompletedTask;
            }
        }

        /// <summary>
        /// Mock folder locator for testing.
        /// </summary>
        private sealed class MockFolderLocator : IDropboxFolderLocator
        {
            public string GetBusinessDropboxRoot()
            {
                return @"C:\Users\Test\Dropbox (Business)";
            }
        }

        /// <summary>
        /// Mock logger for testing.
        /// </summary>
        private sealed class MockLogger : IAppLogger
        {
            public void Info(string message) { }
            public void Warn(string message) { }
            public void Error(Exception ex, string message) { }
        }
    }
}
