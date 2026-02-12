using System.Globalization;
using System.Text;

namespace Service.Utils
{
    /// <summary>
    /// Utility class for text normalization and search operations
    /// </summary>
    public static class TextNormalizer
    {
        /// <summary>
        /// Remove Vietnamese accents/diacritics from text
        /// Converts: "Bánh Cu?n" -> "Banh Cuon"
        /// </summary>
        /// <param name="text">Text with accents</param>
        /// <returns>Text without accents</returns>
        public static string RemoveVietnameseAccents(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            // Normalize to decomposed form (NFD)
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

            // Additional Vietnamese-specific character replacements
            var result = stringBuilder.ToString().Normalize(NormalizationForm.FormC);
            
            result = result.Replace('?', 'd').Replace('?', 'D');
            result = result.Replace('ð', 'd').Replace('Ð', 'D');
            
            return result;
        }

        /// <summary>
        /// Normalize text for search by removing accents and converting to lowercase
        /// </summary>
        public static string NormalizeForSearch(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            return RemoveVietnameseAccents(text).ToLowerInvariant();
        }
    }
}
