using System;
using System.Collections.Generic;
using System.Linq;

namespace Service.Utils
{
    /// <summary>
    /// Pure scoring helpers for the enhanced search ranking capability.
    /// All inputs are expected to be pre-normalized via <see cref="TextNormalizer.NormalizeForSearch"/>.
    /// </summary>
    public static class SearchScorer
    {
        public const double DisplayNameExact = 100.0;
        public const double DisplayNameFuzzy = 80.0;
        public const double DisplayNameFuzzyTypo = 60.0;  // typo-tolerant brand match
        public const double DisplayNameSimilarFloor = 50.0;
        public const double DisplayNameSimilarCeil = 70.0;
        public const double DishExact = 100.0;
        public const double DishCap = 120.0;
        public const double DishFuzzyFloor = 20.0;        // fuzzy dish match floor
        public const double DishFuzzyCeil = 35.0;         // fuzzy dish match ceil
        public const double BestSellerBonus = 10.0;
        public const double HighRatingBonus = 5.0;
        public const double HighRatingThreshold = 4.5;
        public const int HighRatingMinReviews = 20;

        /// <summary>
        /// Score the DisplayName bucket for a single (vendor, branch) pair against the keyword.
        /// Returns 100 for exact match against vendor name, branch name, or vendor+branch DisplayName;
        /// 80 for substring / word-subset fuzzy match; 0 when nothing matches.
        /// The 50–70 Similar bucket is produced separately by <see cref="ScoreSimilar"/>.
        /// </summary>
        public static double ScoreDisplayName(string normalizedKeyword, string? vendorName, string? branchName)
        {
            if (string.IsNullOrEmpty(normalizedKeyword))
                return 0.0;

            var normVendor = TextNormalizer.NormalizeForSearch(vendorName ?? string.Empty);
            var normBranch = TextNormalizer.NormalizeForSearch(branchName ?? string.Empty);
            var normDisplay = string.IsNullOrEmpty(normBranch)
                ? normVendor
                : (string.IsNullOrEmpty(normVendor) ? normBranch : $"{normVendor} {normBranch}");

            if (Exact(normVendor, normalizedKeyword) ||
                Exact(normBranch, normalizedKeyword) ||
                Exact(normDisplay, normalizedKeyword))
            {
                return DisplayNameExact;
            }

            if (Fuzzy(normVendor, normalizedKeyword) ||
                Fuzzy(normBranch, normalizedKeyword) ||
                Fuzzy(normDisplay, normalizedKeyword))
            {
                return DisplayNameFuzzy;
            }

            // Typo-tolerant fallback: every keyword token fuzzy-matches some token in
            // the vendor or branch name (Levenshtein, per-token threshold).
            var kwTokens = TextNormalizer.Tokenize(normalizedKeyword);
            if (TextNormalizer.FuzzyAllTokensMatch(kwTokens, TextNormalizer.Tokenize(normVendor)) ||
                TextNormalizer.FuzzyAllTokensMatch(kwTokens, TextNormalizer.Tokenize(normBranch)))
            {
                return DisplayNameFuzzyTypo;
            }

            return 0.0;
        }

        /// <summary>
        /// Score a dish name against the keyword. Base buckets: 100 exact phrase, 70–90 in-order words,
        /// 40–60 out-of-order words, 0 when any keyword word is missing. Adds +10 best-seller bonus
        /// and +5 high-rating bonus (branch AvgRating ≥ 4.5 AND ≥ 20 reviews). Final value is capped at 120.
        /// </summary>
        public static double ScoreDish(
            string normalizedKeyword,
            string? dishName,
            bool isBestSeller,
            double branchAvgRating,
            int branchReviewCount)
        {
            if (string.IsNullOrEmpty(normalizedKeyword))
                return 0.0;

            var normDish = TextNormalizer.NormalizeForSearch(dishName ?? string.Empty);
            if (string.IsNullOrEmpty(normDish))
                return 0.0;

            var keywordTokens = TextNormalizer.Tokenize(normalizedKeyword);
            var dishTokens = TextNormalizer.Tokenize(normDish);

            double baseScore = 0.0;

            if (Exact(normDish, normalizedKeyword) || PhraseContains(normDish, normalizedKeyword))
            {
                baseScore = DishExact;
            }
            else
            {
                var matchedFraction = MatchedTokenFraction(keywordTokens, dishTokens);
                if (matchedFraction > 0 && ContainsAllTokensInOrder(dishTokens, keywordTokens))
                {
                    // 70 + up to 20 for matchedFraction → cap at 90 to stay strictly below exact.
                    baseScore = Math.Min(90.0, 70.0 + 20.0 * matchedFraction);
                }
                else if (matchedFraction >= 1.0)
                {
                    // Every keyword token appears somewhere, but not in order.
                    baseScore = Math.Min(60.0, 40.0 + 20.0 * matchedFraction);
                }
                else
                {
                    // Fuzzy fallback: fraction of keyword tokens that fuzzy-match a dish token.
                    var fuzzyFraction = TextNormalizer.FuzzyMatchFraction(keywordTokens, dishTokens);
                    if (fuzzyFraction <= 0.0) return 0.0;
                    baseScore = DishFuzzyFloor + (DishFuzzyCeil - DishFuzzyFloor) * fuzzyFraction;
                }
            }

            if (isBestSeller)
                baseScore += BestSellerBonus;

            if (branchAvgRating >= HighRatingThreshold && branchReviewCount >= HighRatingMinReviews)
                baseScore += HighRatingBonus;

            return Math.Min(DishCap, baseScore);
        }

        /// <summary>
        /// Score the Similar (KFC Rule) bucket for a non-anchor vendor against the union of anchor brand signatures.
        /// Produces a value in [50, 70], or 0 when no signature item matched any of the vendor's dishes.
        /// </summary>
        /// <param name="vendorDishNames">All dish names on the candidate vendor's branches (not yet normalized).</param>
        /// <param name="signatureDishNames">Top best-seller dish names from anchor brands (not yet normalized).</param>
        public static double ScoreSimilar(IEnumerable<string> vendorDishNames, IEnumerable<string> signatureDishNames)
        {
            if (vendorDishNames == null || signatureDishNames == null)
                return 0.0;

            var normalizedDishes = vendorDishNames
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Select(TextNormalizer.NormalizeForSearch)
                .Where(n => !string.IsNullOrEmpty(n))
                .ToList();

            if (normalizedDishes.Count == 0)
                return 0.0;

            double matchedSum = 0.0;
            int signatureCount = 0;

            foreach (var raw in signatureDishNames)
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                var signature = TextNormalizer.NormalizeForSearch(raw);
                if (string.IsNullOrEmpty(signature)) continue;
                signatureCount++;

                double bestForSignature = 0.0;
                foreach (var dish in normalizedDishes)
                {
                    var score = ScoreDish(signature, dish, isBestSeller: false, branchAvgRating: 0.0, branchReviewCount: 0);
                    var normalized = Math.Min(1.0, score / DishExact);
                    if (normalized > bestForSignature)
                        bestForSignature = normalized;
                }

                matchedSum += bestForSignature;
            }

            if (signatureCount == 0)
                return 0.0;

            var avgSignatureMatch = Math.Min(1.0, matchedSum / signatureCount);
            if (avgSignatureMatch <= 0.0)
                return 0.0;

            return DisplayNameSimilarFloor + (DisplayNameSimilarCeil - DisplayNameSimilarFloor) * avgSignatureMatch;
        }

        private static bool Exact(string candidate, string keyword)
        {
            return !string.IsNullOrEmpty(candidate) && candidate == keyword;
        }

        private static bool Fuzzy(string candidate, string keyword)
        {
            if (string.IsNullOrEmpty(candidate) || string.IsNullOrEmpty(keyword))
                return false;

            if (candidate.Contains(keyword))
                return true;

            // Word-subset: all keyword tokens present somewhere in candidate tokens.
            var candidateTokens = TextNormalizer.Tokenize(candidate);
            var keywordTokens = TextNormalizer.Tokenize(keyword);
            if (keywordTokens.Length == 0) return false;
            var set = new HashSet<string>(candidateTokens);
            return keywordTokens.All(t => set.Contains(t));
        }

        private static bool PhraseContains(string haystack, string needle)
        {
            return !string.IsNullOrEmpty(haystack) &&
                   !string.IsNullOrEmpty(needle) &&
                   haystack.Contains(needle);
        }

        private static double MatchedTokenFraction(string[] keywordTokens, string[] dishTokens)
        {
            if (keywordTokens.Length == 0) return 0.0;
            var dishSet = new HashSet<string>(dishTokens);
            int matched = keywordTokens.Count(dishSet.Contains);
            return (double)matched / keywordTokens.Length;
        }

        private static bool ContainsAllTokensInOrder(string[] dishTokens, string[] keywordTokens)
        {
            if (keywordTokens.Length == 0) return false;
            int di = 0;
            foreach (var token in keywordTokens)
            {
                while (di < dishTokens.Length && dishTokens[di] != token) di++;
                if (di == dishTokens.Length) return false;
                di++;
            }
            return true;
        }
    }
}
