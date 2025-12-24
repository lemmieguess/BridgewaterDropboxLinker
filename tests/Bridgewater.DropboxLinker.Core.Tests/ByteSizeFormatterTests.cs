using Bridgewater.DropboxLinker.Core.Utilities;
using FluentAssertions;
using Xunit;

namespace Bridgewater.DropboxLinker.Core.Tests
{
    public class ByteSizeFormatterTests
    {
        [Fact]
        public void ToHumanReadable_NegativeValue_ReturnsEmpty()
        {
            ByteSizeFormatter.ToHumanReadable(-1).Should().BeEmpty();
            ByteSizeFormatter.ToHumanReadable(-100).Should().BeEmpty();
        }

        [Theory]
        [InlineData(0, "0 B")]
        [InlineData(1, "1 B")]
        [InlineData(512, "512 B")]
        [InlineData(1023, "1023 B")]
        public void ToHumanReadable_ByteRange_ReturnsBytes(long bytes, string expected)
        {
            ByteSizeFormatter.ToHumanReadable(bytes).Should().Be(expected);
        }

        [Theory]
        [InlineData(1024, "1.0 KB")]
        [InlineData(1536, "1.5 KB")]
        [InlineData(10240, "10.0 KB")]
        [InlineData(1048575, "1024.0 KB")]
        public void ToHumanReadable_KilobyteRange_ReturnsKB(long bytes, string expected)
        {
            ByteSizeFormatter.ToHumanReadable(bytes).Should().Be(expected);
        }

        [Theory]
        [InlineData(1048576, "1.0 MB")]
        [InlineData(1572864, "1.5 MB")]
        [InlineData(10485760, "10.0 MB")]
        [InlineData(1073741823, "1024.0 MB")]
        public void ToHumanReadable_MegabyteRange_ReturnsMB(long bytes, string expected)
        {
            ByteSizeFormatter.ToHumanReadable(bytes).Should().Be(expected);
        }

        [Theory]
        [InlineData(1073741824, "1.0 GB")]
        [InlineData(1610612736, "1.5 GB")]
        [InlineData(10737418240, "10.0 GB")]
        public void ToHumanReadable_GigabyteRange_ReturnsGB(long bytes, string expected)
        {
            ByteSizeFormatter.ToHumanReadable(bytes).Should().Be(expected);
        }

        [Fact]
        public void ToHumanReadable_CommonFileSizes_ReturnsExpected()
        {
            // 5 MB PDF
            ByteSizeFormatter.ToHumanReadable(5 * 1024 * 1024).Should().Be("5.0 MB");

            // 25 MB video
            ByteSizeFormatter.ToHumanReadable(25 * 1024 * 1024).Should().Be("25.0 MB");

            // 2.5 GB archive
            ByteSizeFormatter.ToHumanReadable((long)(2.5 * 1024 * 1024 * 1024)).Should().Be("2.5 GB");
        }
    }
}
