using Service.Utils;
using Xunit;

namespace StreetFood.Tests.Search
{
    public class SearchScorerTests
    {
        // --- DisplayName scoring ---

        [Fact]
        public void ScoreDisplayName_ExactVendorMatch_Returns100()
        {
            var score = SearchScorer.ScoreDisplayName("kfc", originalKeyword: null, "KFC", "Nguyễn Trãi");
            Assert.Equal(100.0, score);
        }

        [Fact]
        public void ScoreDisplayName_ExactDisplayNameMatch_Returns100()
        {
            var score = SearchScorer.ScoreDisplayName("kfc nguyen trai", originalKeyword: null, "KFC", "Nguyễn Trãi");
            Assert.Equal(100.0, score);
        }

        [Fact]
        public void ScoreDisplayName_FuzzyVendorSubstring_Returns80()
        {
            var score = SearchScorer.ScoreDisplayName("pizza", originalKeyword: null, "Pizza Hut", "Quận 1");
            Assert.Equal(80.0, score);
        }

        [Fact]
        public void ScoreDisplayName_NoMatch_Returns0()
        {
            var score = SearchScorer.ScoreDisplayName("burger", originalKeyword: null, "Pizza Hut", "Quận 1");
            Assert.Equal(0.0, score);
        }

        [Fact]
        public void ScoreDisplayName_EmptyKeyword_Returns0()
        {
            var score = SearchScorer.ScoreDisplayName(string.Empty, originalKeyword: null, "KFC", "Nguyễn Trãi");
            Assert.Equal(0.0, score);
        }

        [Fact]
        public void ScoreDisplayName_DiacriticExact_AddsBonus()
        {
            // User typed with diacritics, vendor name contains the literal keyword → +15 bonus.
            var withBonus = SearchScorer.ScoreDisplayName("pho", originalKeyword: "Phở", "Phở Hùng", "Quận 1");
            var noBonus = SearchScorer.ScoreDisplayName("pho", originalKeyword: "Phở", "Phô Mai Vivian", "Quận 1");
            Assert.Equal(SearchScorer.DisplayNameFuzzy + SearchScorer.DiacriticExactBonus, withBonus);
            Assert.Equal(SearchScorer.DisplayNameFuzzy, noBonus);
        }

        [Fact]
        public void ScoreDisplayName_NoDiacriticInKeyword_NoBonus()
        {
            // User typed without diacritics → bonus disabled, both vendors score equally.
            var phoVendor = SearchScorer.ScoreDisplayName("pho", originalKeyword: "pho", "Phở Hùng", "Quận 1");
            var phoMaiVendor = SearchScorer.ScoreDisplayName("pho", originalKeyword: "pho", "Phô Mai Vivian", "Quận 1");
            Assert.Equal(phoVendor, phoMaiVendor);
        }

        // --- Dish scoring ---

        [Fact]
        public void ScoreDish_ExactPhrase_Returns100Base()
        {
            var score = SearchScorer.ScoreDish(
                normalizedKeyword: "bun bo hue",
                originalKeyword: null,
                dishName: "Bún bò Huế",
                isBestSeller: false,
                branchAvgRating: 0.0,
                branchReviewCount: 0);
            Assert.Equal(100.0, score);
        }

        [Fact]
        public void ScoreDish_InOrderWords_ScoresInSeventyToNinetyRange()
        {
            // Keyword "broken rice" appears in order but not contiguously in "broken and rice".
            var score = SearchScorer.ScoreDish("broken rice", originalKeyword: null, "broken and rice",
                isBestSeller: false, branchAvgRating: 0.0, branchReviewCount: 0);
            Assert.InRange(score, 70.0, 90.0);
        }

        [Fact]
        public void ScoreDish_InOrderBeatsOutOfOrder()
        {
            var inOrder = SearchScorer.ScoreDish("broken rice", null, "broken and rice", false, 0.0, 0);
            var outOfOrder = SearchScorer.ScoreDish("broken rice", null, "rice then broken", false, 0.0, 0);
            Assert.True(inOrder > outOfOrder,
                $"Expected in-order ({inOrder}) to outrank out-of-order ({outOfOrder})");
        }

        [Fact]
        public void ScoreDish_OutOfOrderWords_ScoresInFortyToSixtyRange()
        {
            var score = SearchScorer.ScoreDish("broken rice", originalKeyword: null, "rice then broken",
                isBestSeller: false, branchAvgRating: 0.0, branchReviewCount: 0);
            Assert.InRange(score, 40.0, 60.0);
        }

        [Fact]
        public void ScoreDish_MissingWord_Returns0()
        {
            var score = SearchScorer.ScoreDish("broken rice", originalKeyword: null, "fried noodles",
                isBestSeller: false, branchAvgRating: 0.0, branchReviewCount: 0);
            Assert.Equal(0.0, score);
        }

        [Fact]
        public void ScoreDish_BestSellerBonusApplied()
        {
            var baseScore = SearchScorer.ScoreDish("ga ran", null, "Gà rán", false, 0.0, 0);
            var withBonus = SearchScorer.ScoreDish("ga ran", null, "Gà rán", true, 0.0, 0);
            Assert.Equal(baseScore + SearchScorer.BestSellerBonus, withBonus);
        }

        [Fact]
        public void ScoreDish_HighRatingBonusRequiresBothThresholds()
        {
            var baseScore = SearchScorer.ScoreDish("ga ran", null, "Gà rán", false, 0.0, 0);
            // Rating high enough but too few reviews → no bonus.
            var lowReviews = SearchScorer.ScoreDish("ga ran", null, "Gà rán", false, 4.8, 19);
            Assert.Equal(baseScore, lowReviews);

            // Rating below threshold → no bonus.
            var lowRating = SearchScorer.ScoreDish("ga ran", null, "Gà rán", false, 4.4, 100);
            Assert.Equal(baseScore, lowRating);

            // Both thresholds met → bonus.
            var withBonus = SearchScorer.ScoreDish("ga ran", null, "Gà rán", false, 4.5, 20);
            Assert.Equal(baseScore + SearchScorer.HighRatingBonus, withBonus);
        }

        [Fact]
        public void ScoreDish_CappedAtDishCap()
        {
            // Exact (100) + diacritic (+15) + best-seller (+10) + high-rating (+5) = 130 ≤ DishCap (140).
            var score = SearchScorer.ScoreDish("ga ran", originalKeyword: "Gà rán", "Gà rán",
                isBestSeller: true, branchAvgRating: 5.0, branchReviewCount: 1000);
            Assert.True(score <= SearchScorer.DishCap,
                $"Expected dishScore ({score}) <= {SearchScorer.DishCap}");
            Assert.Equal(130.0, score); // 100 + 15 + 10 + 5
        }

        [Fact]
        public void ScoreDish_DiacriticExact_AddsBonus()
        {
            // User typed "Phở" (with diacritics). Dish "Phở Bò" matches literally, "Phô Mai" does not.
            var phoBo = SearchScorer.ScoreDish("pho", originalKeyword: "Phở", "Phở Bò", false, 0.0, 0);
            var phoMai = SearchScorer.ScoreDish("pho", originalKeyword: "Phở", "Phô Mai", false, 0.0, 0);
            Assert.True(phoBo > phoMai, $"Expected Phở Bò ({phoBo}) > Phô Mai ({phoMai}) when keyword has diacritics");
            Assert.Equal(SearchScorer.DishExact + SearchScorer.DiacriticExactBonus, phoBo);
            Assert.Equal(SearchScorer.DishExact, phoMai);
        }

        [Fact]
        public void ScoreDish_NoDiacriticInKeyword_BothEqual()
        {
            // User typed plain "pho" → no preference between Phở and Phô.
            var phoBo = SearchScorer.ScoreDish("pho", originalKeyword: "pho", "Phở Bò", false, 0.0, 0);
            var phoMai = SearchScorer.ScoreDish("pho", originalKeyword: "pho", "Phô Mai", false, 0.0, 0);
            Assert.Equal(phoBo, phoMai);
        }

        // --- Similar (KFC Rule) scoring ---

        [Fact]
        public void ScoreSimilar_MatchingSignature_LandsInFiftyToSeventy()
        {
            // Anchor KFC's signatures include "ga ran"; candidate Jollibee sells "Gà rán giòn".
            var signatures = new[] { "Gà rán", "Burger" };
            var jollibeeDishes = new[] { "Gà rán giòn", "Spaghetti sốt cà" };
            var score = SearchScorer.ScoreSimilar(jollibeeDishes, signatures);
            Assert.InRange(score, 50.0, 70.0);
        }

        [Fact]
        public void ScoreSimilar_NoSignatureMatch_ReturnsZero()
        {
            var signatures = new[] { "Gà rán", "Burger" };
            var dishes = new[] { "Phở bò", "Bún chả" };
            var score = SearchScorer.ScoreSimilar(dishes, signatures);
            Assert.Equal(0.0, score);
        }

        [Fact]
        public void ScoreSimilar_MoreMatchesProduceHigherScore()
        {
            var signatures = new[] { "Gà rán", "Burger", "Khoai tây" };
            var oneMatch = new[] { "Gà rán giòn", "Phở bò" };
            var twoMatches = new[] { "Gà rán giòn", "Burger bò" };
            var s1 = SearchScorer.ScoreSimilar(oneMatch, signatures);
            var s2 = SearchScorer.ScoreSimilar(twoMatches, signatures);
            Assert.True(s2 > s1, $"Expected two-match score ({s2}) to exceed one-match ({s1})");
        }

        [Fact]
        public void ScoreSimilar_NeverExceedsFuzzyBucket()
        {
            var signatures = new[] { "Gà rán", "Burger" };
            var perfect = new[] { "Gà rán", "Burger" };
            var score = SearchScorer.ScoreSimilar(perfect, signatures);
            Assert.InRange(score, 50.0, 70.0);
            Assert.True(score < SearchScorer.DisplayNameFuzzy,
                $"Similar score ({score}) must stay below fuzzy bucket ({SearchScorer.DisplayNameFuzzy})");
        }

        // ----- DisplayName scoring — Trà sữa cases ----------------------------

        [Fact]
        public void ScoreDisplayName_TraSua_ExactVendorName_Returns100()
        {
            // Quán tên đúng "Trà Sữa" → exact
            var score = SearchScorer.ScoreDisplayName("tra sua", originalKeyword: null, "Trà Sữa", "Quận 1");
            Assert.Equal(100.0, score);
        }

        [Fact]
        public void ScoreDisplayName_TraSua_VendorContainsKeyword_Returns80()
        {
            // "Trà Sữa Minh" chứa "tra sua" → fuzzy substring
            var score = SearchScorer.ScoreDisplayName("tra sua", originalKeyword: null, "Trà Sữa Minh", "Quận 3");
            Assert.Equal(80.0, score);
        }

        [Fact]
        public void ScoreDisplayName_TraSua_GongCha_Returns0()
        {
            // Brand lớn không có "tra sua" trong tên → không khớp display name
            var score = SearchScorer.ScoreDisplayName("tra sua", originalKeyword: null, "Gong Cha", "Quận 1");
            Assert.Equal(0.0, score);
        }

        [Fact]
        public void ScoreDisplayName_TraSua_TheAlley_Returns0()
        {
            var score = SearchScorer.ScoreDisplayName("tra sua", originalKeyword: null, "The Alley", "Quận 1");
            Assert.Equal(0.0, score);
        }

        [Fact]
        public void ScoreDisplayName_TraSua_PhucLong_Returns0()
        {
            var score = SearchScorer.ScoreDisplayName("tra sua", originalKeyword: null, "Phúc Long", "Quận 1");
            Assert.Equal(0.0, score);
        }
    }
}
