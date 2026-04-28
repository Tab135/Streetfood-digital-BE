using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace Service.Utils
{
    public static class TextNormalizer
    {
        private static readonly object _synonymLock = new();
        private static Dictionary<string, HashSet<string>> _expansions = new();

        static TextNormalizer()
        {
            LoadSynonymsFromEmbeddedResource();
        }

        public static string RemoveVietnameseAccents(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();

            foreach (var c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            var result = stringBuilder.ToString().Normalize(NormalizationForm.FormC);

            result = result.Replace('Đ', 'D').Replace('đ', 'd');

            return result;
        }

        /// <summary>
        /// Normalize text for search: strip Vietnamese accents, lowercase, collapse
        /// internal whitespace to single spaces, trim.
        /// </summary>
        public static string NormalizeForSearch(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            var stripped = RemoveVietnameseAccents(text).ToLowerInvariant();
            var tokens = stripped
                .Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            return string.Join(' ', tokens);
        }

        /// <summary>
        /// Split normalized text into whitespace-separated tokens.
        /// </summary>
        public static string[] Tokenize(string normalizedText)
        {
            if (string.IsNullOrWhiteSpace(normalizedText))
                return Array.Empty<string>();

            return normalizedText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>
        /// Levenshtein edit distance between two strings.
        /// </summary>
        public static int EditDistance(string a, string b)
        {
            if (string.IsNullOrEmpty(a)) return b?.Length ?? 0;
            if (string.IsNullOrEmpty(b)) return a.Length;

            int m = a.Length, n = b.Length;
            var dp = new int[m + 1, n + 1];
            for (int i = 0; i <= m; i++) dp[i, 0] = i;
            for (int j = 0; j <= n; j++) dp[0, j] = j;
            for (int i = 1; i <= m; i++)
                for (int j = 1; j <= n; j++)
                    dp[i, j] = a[i - 1] == b[j - 1]
                        ? dp[i - 1, j - 1]
                        : 1 + Math.Min(dp[i - 1, j - 1], Math.Min(dp[i - 1, j], dp[i, j - 1]));
            return dp[m, n];
        }

        // Tokens ≤ 2 chars: no fuzzy (too short, too many false positives).
        // Tokens 3–4 chars: allow 1 typo. Tokens ≥ 5 chars: allow 2 typos.
        private static int FuzzyThreshold(int tokenLength)
        {
            if (tokenLength <= 2) return 0;
            if (tokenLength <= 4) return 1;
            return 2;
        }

        /// <summary>
        /// Returns the fraction of keywordTokens that fuzzy-match at least one candidateToken,
        /// using per-token Levenshtein thresholds. Tokens ≤ 2 chars are excluded from fuzzy
        /// (threshold 0) and count as unmatched when computing the fraction.
        /// </summary>
        public static double FuzzyMatchFraction(string[] keywordTokens, string[] candidateTokens)
        {
            if (keywordTokens.Length == 0) return 0.0;
            int matched = 0;
            foreach (var kw in keywordTokens)
            {
                int threshold = FuzzyThreshold(kw.Length);
                if (threshold == 0) continue;
                foreach (var cd in candidateTokens)
                {
                    if (EditDistance(kw, cd) <= threshold) { matched++; break; }
                }
            }
            return (double)matched / keywordTokens.Length;
        }

        /// <summary>
        /// Returns true when every keyword token fuzzy-matches some candidate token.
        /// Tokens ≤ 2 chars require an exact candidate token match (threshold 0).
        /// </summary>
        public static bool FuzzyAllTokensMatch(string[] keywordTokens, string[] candidateTokens)
        {
            if (keywordTokens.Length == 0) return false;
            foreach (var kw in keywordTokens)
            {
                int threshold = FuzzyThreshold(kw.Length);
                bool found = false;
                foreach (var cd in candidateTokens)
                {
                    if (EditDistance(kw, cd) <= threshold) { found = true; break; }
                }
                if (!found) return false;
            }
            return true;
        }

        /// <summary>
        /// Given a normalized keyword, return every synonym-equivalent form (including the keyword itself).
        /// When no synonym entry matches, returns a set containing only the input keyword.
        /// </summary>
        public static HashSet<string> ExpandWithSynonyms(string normalizedKeyword)
        {
            var result = new HashSet<string>(StringComparer.Ordinal);
            if (string.IsNullOrWhiteSpace(normalizedKeyword))
                return result;

            result.Add(normalizedKeyword);

            if (_expansions.TryGetValue(normalizedKeyword, out var equivalents))
            {
                foreach (var alt in equivalents)
                    result.Add(alt);
            }

            return result;
        }

        /// <summary>
        /// Load synonyms from a JSON file on disk. Intended for tests or future CMS integration.
        /// JSON shape: { "synonyms": { "canonical": ["alt1", "alt2"] } }.
        /// </summary>
        public static void LoadSynonyms(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                lock (_synonymLock) { _expansions = new Dictionary<string, HashSet<string>>(); }
                return;
            }

            using var stream = File.OpenRead(filePath);
            LoadSynonymsFromStream(stream);
        }

        private static void LoadSynonymsFromEmbeddedResource()
        {
            try
            {
                var assembly = typeof(TextNormalizer).Assembly;
                var resourceName = assembly.GetManifestResourceNames()
                    .FirstOrDefault(n => n.EndsWith("SearchSynonyms.json", StringComparison.Ordinal));
                if (resourceName == null)
                    return;

                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream == null)
                    return;

                LoadSynonymsFromStream(stream);
            }
            catch
            {
                // Fallback to an empty map if the resource can't be parsed — search still works without synonyms.
                lock (_synonymLock) { _expansions = new Dictionary<string, HashSet<string>>(); }
            }
        }

        private static void LoadSynonymsFromStream(Stream stream)
        {
            using var doc = JsonDocument.Parse(stream);
            if (!doc.RootElement.TryGetProperty("synonyms", out var synonymsElement) ||
                synonymsElement.ValueKind != JsonValueKind.Object)
            {
                lock (_synonymLock) { _expansions = new Dictionary<string, HashSet<string>>(); }
                return;
            }

            // Build canonical equivalence classes: every form in a row is equivalent to every other form in that row.
            var rowMap = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
            foreach (var entry in synonymsElement.EnumerateObject())
            {
                var canonical = NormalizeForSearch(entry.Name);
                if (string.IsNullOrEmpty(canonical)) continue;

                var row = new HashSet<string>(StringComparer.Ordinal) { canonical };
                if (entry.Value.ValueKind == JsonValueKind.Array)
                {
                    foreach (var alt in entry.Value.EnumerateArray())
                    {
                        if (alt.ValueKind != JsonValueKind.String) continue;
                        var normalizedAlt = NormalizeForSearch(alt.GetString() ?? string.Empty);
                        if (!string.IsNullOrEmpty(normalizedAlt))
                            row.Add(normalizedAlt);
                    }
                }

                foreach (var form in row)
                {
                    if (!rowMap.TryGetValue(form, out var bucket))
                    {
                        bucket = new HashSet<string>(StringComparer.Ordinal);
                        rowMap[form] = bucket;
                    }
                    foreach (var other in row)
                        bucket.Add(other);
                }
            }

            lock (_synonymLock) { _expansions = rowMap; }
        }
    }
}
