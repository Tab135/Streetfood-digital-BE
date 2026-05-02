using Service.Utils;
using Xunit;

namespace StreetFood.Tests.Search
{
    public class TextNormalizerTests
    {
        [Theory]
        [InlineData("Cơm Tấm", "com tam")]
        [InlineData("COM TAM", "com tam")]
        [InlineData("Cơm tấm", "com tam")]
        [InlineData("  Phở   Bò  ", "pho bo")]
        public void NormalizeForSearch_StripsAccentsAndCollapsesWhitespace(string input, string expected)
        {
            Assert.Equal(expected, TextNormalizer.NormalizeForSearch(input));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void NormalizeForSearch_EmptyInputReturnsEmpty(string? input)
        {
            Assert.Equal(string.Empty, TextNormalizer.NormalizeForSearch(input!));
        }

        [Fact]
        public void Tokenize_SplitsOnWhitespace()
        {
            var tokens = TextNormalizer.Tokenize("broken rice with pork");
            Assert.Equal(new[] { "broken", "rice", "with", "pork" }, tokens);
        }

        [Fact]
        public void Tokenize_EmptyReturnsEmptyArray()
        {
            Assert.Empty(TextNormalizer.Tokenize(string.Empty));
            Assert.Empty(TextNormalizer.Tokenize("   "));
        }

        [Fact]
        public void ExpandWithSynonyms_IncludesCanonicalAndAlternates()
        {
            // Default synonym map loads from embedded Service/Utils/SearchSynonyms.json
            // and defines "com tam" <-> "com suon" <-> "com suon nuong" <-> "com bi cha".
            var normalized = TextNormalizer.NormalizeForSearch("Cơm tấm");
            var expansions = TextNormalizer.ExpandWithSynonyms(normalized);

            Assert.Contains("com tam", expansions);
            Assert.Contains("com suon", expansions);
        }

        [Fact]
        public void ExpandWithSynonyms_ReverseDirectionAlsoWorks()
        {
            var normalized = TextNormalizer.NormalizeForSearch("Cơm sườn");
            var expansions = TextNormalizer.ExpandWithSynonyms(normalized);

            Assert.Contains("com suon", expansions);
            Assert.Contains("com tam", expansions);
        }

        [Fact]
        public void ExpandWithSynonyms_UnknownKeywordReturnsSelfOnly()
        {
            var expansions = TextNormalizer.ExpandWithSynonyms("xyz unknown");
            Assert.Single(expansions);
            Assert.Contains("xyz unknown", expansions);
        }

        [Fact]
        public void ExpandWithSynonyms_EmptyInputReturnsEmpty()
        {
            Assert.Empty(TextNormalizer.ExpandWithSynonyms(string.Empty));
        }

        // ----- Trà sữa synonym expansion (no brand names) ----------------------

        [Fact]
        public void ExpandWithSynonyms_TraSua_IncludesFoodTypeForms()
        {
            var expansions = TextNormalizer.ExpandWithSynonyms(
                TextNormalizer.NormalizeForSearch("Trà sữa"));

            Assert.Contains("tra sua",          expansions);
            Assert.Contains("milk tea",         expansions);
            Assert.Contains("bubble tea",       expansions);
            Assert.Contains("matcha",           expansions);
            Assert.Contains("tra sua tran chau", expansions);
        }

        [Fact]
        public void ExpandWithSynonyms_TraSua_DoesNotContainBrandNames()
        {
            var expansions = TextNormalizer.ExpandWithSynonyms(
                TextNormalizer.NormalizeForSearch("Trà sữa"));

            // Brand names không được nằm trong synonym expansion
            Assert.DoesNotContain("gong cha",   expansions);
            Assert.DoesNotContain("the alley",  expansions);
            Assert.DoesNotContain("phuc long",  expansions);
        }

        [Fact]
        public void ExpandWithSynonyms_MilkTea_ResolvesBidirectionalToTraSua()
        {
            var expansions = TextNormalizer.ExpandWithSynonyms("milk tea");

            Assert.Contains("tra sua",    expansions);
            Assert.Contains("bubble tea", expansions);
            Assert.Contains("matcha",     expansions);
        }
    }
}
