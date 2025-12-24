using Bridgewater.DropboxLinker.Core.Utilities;
using FluentAssertions;
using Xunit;

namespace Bridgewater.DropboxLinker.Core.Tests
{
    public class FileNameCleanerTests
    {
        [Theory]
        [InlineData(null, "")]
        [InlineData("", "")]
        [InlineData("   ", "")]
        public void CleanForDisplay_HandlesNullOrEmpty(string? input, string expected)
        {
            FileNameCleaner.CleanForDisplay(input).Should().Be(expected);
        }

        [Theory]
        [InlineData("simple_file.pdf", "Simple File.pdf")]
        [InlineData("some-document.docx", "Some Document.docx")]
        [InlineData("file_with-mixed_separators.xlsx", "File With Mixed Separators.xlsx")]
        public void CleanForDisplay_ReplacesSeparatorsWithSpaces(string input, string expected)
        {
            FileNameCleaner.CleanForDisplay(input).Should().Be(expected);
        }

        [Theory]
        [InlineData("report_v1.pdf", "Report.pdf")]
        [InlineData("document_v12.docx", "Document.docx")]
        [InlineData("file_rev3.xlsx", "File.xlsx")]
        [InlineData("budget_r2.pdf", "Budget.pdf")]
        public void CleanForDisplay_StripsVersionTokens(string input, string expected)
        {
            FileNameCleaner.CleanForDisplay(input).Should().Be(expected);
        }

        [Theory]
        [InlineData("proposal_draft.pdf", "Proposal.pdf")]
        [InlineData("contract_copy.docx", "Contract.docx")]
        [InlineData("report_final.xlsx", "Report.xlsx")]
        [InlineData("doc_final_final.pdf", "Doc.pdf")]
        public void CleanForDisplay_StripsDraftCopyFinalTokens(string input, string expected)
        {
            FileNameCleaner.CleanForDisplay(input).Should().Be(expected);
        }

        [Theory]
        [InlineData("my   file.pdf", "My File.pdf")]
        [InlineData("too    many     spaces.docx", "Too Many Spaces.docx")]
        public void CleanForDisplay_CollapsesWhitespace(string input, string expected)
        {
            FileNameCleaner.CleanForDisplay(input).Should().Be(expected);
        }

        [Theory]
        [InlineData("ALL CAPS FILE.pdf", "All Caps File.pdf")]
        [InlineData("lowercase file.docx", "Lowercase File.docx")]
        [InlineData("MiXeD cAsE.xlsx", "Mixed Case.xlsx")]
        public void CleanForDisplay_AppliesTitleCase(string input, string expected)
        {
            FileNameCleaner.CleanForDisplay(input).Should().Be(expected);
        }

        [Theory]
        [InlineData("file.pdf", ".pdf")]
        [InlineData("document.DOCX", ".DOCX")]
        [InlineData("archive.tar.gz", ".gz")]
        public void CleanForDisplay_PreservesExtension(string input, string expectedExtension)
        {
            var result = FileNameCleaner.CleanForDisplay(input);
            result.Should().EndWith(expectedExtension);
        }

        [Fact]
        public void CleanForDisplay_HandlesComplexFileName()
        {
            var input = "bridgewater_proposal_v3_draft_rev2.pdf";
            var result = FileNameCleaner.CleanForDisplay(input);
            result.Should().Be("Bridgewater Proposal.pdf");
        }

        [Fact]
        public void CleanForDisplay_HandlesFileWithNoExtension()
        {
            var input = "readme_file_v1";
            var result = FileNameCleaner.CleanForDisplay(input);
            result.Should().Be("Readme File");
        }
    }
}
