using System;
using Bridgewater.DropboxLinker.Core.Contracts;
using Bridgewater.DropboxLinker.Core.Html;
using FluentAssertions;
using Xunit;

namespace Bridgewater.DropboxLinker.Core.Tests
{
    public class LinkBlockBuilderTests
    {
        private readonly LinkBlockBuilder _builder = new LinkBlockBuilder();

        [Fact]
        public void BuildHtmlBlock_NullLink_ThrowsArgumentNullException()
        {
            var act = () => _builder.BuildHtmlBlock(null!, 1024);
            act.Should().Throw<ArgumentNullException>().WithParameterName("link");
        }

        [Fact]
        public void BuildPlainTextBlock_NullLink_ThrowsArgumentNullException()
        {
            var act = () => _builder.BuildPlainTextBlock(null!, 1024);
            act.Should().Throw<ArgumentNullException>().WithParameterName("link");
        }

        [Fact]
        public void BuildHtmlBlock_ContainsRequiredElements()
        {
            var link = new LinkResult
            {
                Url = "https://www.dropbox.com/s/abc123/test.pdf",
                DisplayName = "Test Document.pdf",
                DropboxPath = "/Test Document.pdf"
            };

            var html = _builder.BuildHtmlBlock(link, 1024 * 1024);

            html.Should().Contain("Dropbox link");
            html.Should().Contain("Test Document.pdf");
            html.Should().Contain("1.0 MB");
            html.Should().Contain("Open");
            html.Should().Contain("https://www.dropbox.com/s/abc123/test.pdf");
        }

        [Fact]
        public void BuildHtmlBlock_UsesTableLayout()
        {
            var link = new LinkResult
            {
                Url = "https://example.com",
                DisplayName = "File.pdf"
            };

            var html = _builder.BuildHtmlBlock(link, 1024);

            html.Should().Contain("<table");
            html.Should().Contain("role=\"presentation\"");
            html.Should().Contain("</table>");
        }

        [Fact]
        public void BuildHtmlBlock_EscapesHtmlInDisplayName()
        {
            var link = new LinkResult
            {
                Url = "https://example.com",
                DisplayName = "File <script>alert('xss')</script>.pdf"
            };

            var html = _builder.BuildHtmlBlock(link, 1024);

            html.Should().NotContain("<script>");
            html.Should().Contain("&lt;script&gt;");
        }

        [Fact]
        public void BuildHtmlBlock_EscapesQuotesInUrl()
        {
            var link = new LinkResult
            {
                Url = "https://example.com/file?name=\"test\"",
                DisplayName = "File.pdf"
            };

            var html = _builder.BuildHtmlBlock(link, 1024);

            html.Should().Contain("&quot;test&quot;");
        }

        [Fact]
        public void BuildPlainTextBlock_ContainsRequiredElements()
        {
            var link = new LinkResult
            {
                Url = "https://www.dropbox.com/s/abc123/test.pdf",
                DisplayName = "Test Document.pdf"
            };

            var text = _builder.BuildPlainTextBlock(link, 5 * 1024 * 1024);

            text.Should().Contain("Dropbox link");
            text.Should().Contain("Test Document.pdf");
            text.Should().Contain("5.0 MB");
            text.Should().Contain("Open:");
            text.Should().Contain("https://www.dropbox.com/s/abc123/test.pdf");
        }

        [Fact]
        public void BuildPlainTextBlock_UsesWindowsLineEndings()
        {
            var link = new LinkResult
            {
                Url = "https://example.com",
                DisplayName = "File.pdf"
            };

            var text = _builder.BuildPlainTextBlock(link, 1024);

            text.Should().Contain("\r\n");
        }

        [Fact]
        public void BuildHtmlBlock_HandlesEmptyDisplayName()
        {
            var link = new LinkResult
            {
                Url = "https://example.com",
                DisplayName = ""
            };

            var html = _builder.BuildHtmlBlock(link, 1024);

            html.Should().NotBeEmpty();
            html.Should().Contain("<strong></strong>");
        }

        [Fact]
        public void BuildHtmlBlock_HandlesZeroFileSize()
        {
            var link = new LinkResult
            {
                Url = "https://example.com",
                DisplayName = "Empty.txt"
            };

            var html = _builder.BuildHtmlBlock(link, 0);

            html.Should().Contain("0 B");
        }
    }
}
