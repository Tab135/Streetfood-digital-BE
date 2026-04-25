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
