using System;
using Bridgewater.DropboxLinker.Core.Dropbox;
using FluentAssertions;
using Xunit;

namespace Bridgewater.DropboxLinker.Core.Tests
{
    public class DropboxPathMapperTests
    {
        private readonly DropboxPathMapper _mapper = new DropboxPathMapper();

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void ToDropboxPath_NullOrEmptyRoot_ThrowsArgumentException(string? root)
        {
            var act = () => _mapper.ToDropboxPath(root!, @"C:\file.txt");
            act.Should().Throw<ArgumentException>().WithParameterName("dropboxRootLocalPath");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void ToDropboxPath_NullOrEmptyLocalPath_ThrowsArgumentException(string? localPath)
        {
            var act = () => _mapper.ToDropboxPath(@"C:\Dropbox", localPath!);
            act.Should().Throw<ArgumentException>().WithParameterName("localPath");
        }

        [Fact]
        public void ToDropboxPath_FileOutsideRoot_ThrowsInvalidOperationException()
        {
            var root = @"C:\Dropbox\Business";
            var localPath = @"D:\Other\file.pdf";

            var act = () => _mapper.ToDropboxPath(root, localPath);
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*not inside*Dropbox*");
        }

        [Theory]
        [InlineData(@"C:\Dropbox", @"C:\Dropbox\file.pdf", "/file.pdf")]
        [InlineData(@"C:\Dropbox\", @"C:\Dropbox\file.pdf", "/file.pdf")]
        [InlineData(@"C:\Dropbox", @"C:\Dropbox\folder\file.pdf", "/folder/file.pdf")]
        [InlineData(@"C:\Dropbox", @"C:\Dropbox\a\b\c\file.pdf", "/a/b/c/file.pdf")]
        public void ToDropboxPath_ValidPaths_ReturnsDropboxPath(string root, string localPath, string expected)
        {
            _mapper.ToDropboxPath(root, localPath).Should().Be(expected);
        }

        [Fact]
        public void ToDropboxPath_HandlesTrailingSlashInRoot()
        {
            var result1 = _mapper.ToDropboxPath(@"C:\Dropbox", @"C:\Dropbox\test.pdf");
            var result2 = _mapper.ToDropboxPath(@"C:\Dropbox\", @"C:\Dropbox\test.pdf");

            result1.Should().Be("/test.pdf");
            result2.Should().Be("/test.pdf");
        }

        [Fact]
        public void ToDropboxPath_NormalizesPathSeparators()
        {
            // Mixed separators should be normalized
            var root = @"C:\Dropbox";
            var localPath = @"C:\Dropbox/folder\subfolder/file.pdf";

            var result = _mapper.ToDropboxPath(root, localPath);
            result.Should().Be("/folder/subfolder/file.pdf");
        }

        [Theory]
        [InlineData(@"C:\Dropbox", @"C:\dropbox\file.pdf", "/file.pdf")]
        [InlineData(@"c:\dropbox", @"C:\Dropbox\File.PDF", "/File.PDF")]
        public void ToDropboxPath_IsCaseInsensitive(string root, string localPath, string expected)
        {
            // Windows paths are case-insensitive
            _mapper.ToDropboxPath(root, localPath).Should().Be(expected);
        }

        [Fact]
        public void IsInsideDropboxRoot_ValidPath_ReturnsTrue()
        {
            var root = @"C:\Dropbox";
            var localPath = @"C:\Dropbox\subfolder\file.pdf";

            _mapper.IsInsideDropboxRoot(root, localPath).Should().BeTrue();
        }

        [Fact]
        public void IsInsideDropboxRoot_InvalidPath_ReturnsFalse()
        {
            var root = @"C:\Dropbox";
            var localPath = @"D:\Other\file.pdf";

            _mapper.IsInsideDropboxRoot(root, localPath).Should().BeFalse();
        }

        [Theory]
        [InlineData(null, @"C:\file.txt")]
        [InlineData(@"C:\Dropbox", null)]
        [InlineData("", @"C:\file.txt")]
        [InlineData(@"C:\Dropbox", "")]
        public void IsInsideDropboxRoot_NullOrEmptyInputs_ReturnsFalse(string? root, string? localPath)
        {
            _mapper.IsInsideDropboxRoot(root!, localPath!).Should().BeFalse();
        }
    }
}
