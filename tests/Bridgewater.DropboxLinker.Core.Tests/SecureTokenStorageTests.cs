using Bridgewater.DropboxLinker.Core.Auth;
using FluentAssertions;
using Xunit;

namespace Bridgewater.DropboxLinker.Core.Tests
{
    /// <summary>
    /// Tests for SecureTokenStorage.
    /// Note: These tests require Windows Credential Manager access.
    /// They may be skipped in CI environments.
    /// </summary>
    [Trait("Category", "Integration")]
    public class SecureTokenStorageTests
    {
        private const string TestToken = "test_refresh_token_12345";

        [Fact]
        public void StoreRefreshToken_ReturnsFalseForNullToken()
        {
            // Arrange
            var storage = new SecureTokenStorage();

            // Act
            var result = storage.StoreRefreshToken(null!);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void StoreRefreshToken_ReturnsFalseForEmptyToken()
        {
            // Arrange
            var storage = new SecureTokenStorage();

            // Act
            var result = storage.StoreRefreshToken(string.Empty);

            // Assert
            result.Should().BeFalse();
        }

        [SkippableFact]
        public void RoundTrip_StoreAndRetrieveToken()
        {
            // Skip if not on Windows
            Skip.IfNot(System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
                System.Runtime.InteropServices.OSPlatform.Windows),
                "Credential Manager is Windows-only");

            // Arrange
            var storage = new SecureTokenStorage();
            
            try
            {
                // Act
                var storeResult = storage.StoreRefreshToken(TestToken);
                var retrievedToken = storage.GetRefreshToken();

                // Assert
                storeResult.Should().BeTrue();
                retrievedToken.Should().Be(TestToken);
            }
            finally
            {
                // Cleanup
                storage.DeleteRefreshToken();
            }
        }

        [SkippableFact]
        public void DeleteRefreshToken_RemovesStoredToken()
        {
            // Skip if not on Windows
            Skip.IfNot(System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
                System.Runtime.InteropServices.OSPlatform.Windows),
                "Credential Manager is Windows-only");

            // Arrange
            var storage = new SecureTokenStorage();
            storage.StoreRefreshToken(TestToken);

            // Act
            var deleteResult = storage.DeleteRefreshToken();
            var retrievedToken = storage.GetRefreshToken();

            // Assert
            deleteResult.Should().BeTrue();
            retrievedToken.Should().BeNull();
        }

        [Fact]
        public void GetRefreshToken_ReturnsNullWhenNotStored()
        {
            // Arrange
            var storage = new SecureTokenStorage();
            
            // Ensure no token exists
            storage.DeleteRefreshToken();

            // Act
            var token = storage.GetRefreshToken();

            // Assert
            token.Should().BeNull();
        }
    }
}
