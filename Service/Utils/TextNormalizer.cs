using System.Globalization;
using System.Text;

namespace Service.Utils
{
    public static class TextNormalizer
    {
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

            // Additional Vietnamese-specific character replacements
            var result = stringBuilder.ToString().Normalize(NormalizationForm.FormC);
            
            result = result.Replace('?', 'd').Replace('?', 'D');
            
            return result;
        }

        /// Normalize text for search by removing accents and converting to lowercase
        public static string NormalizeForSearch(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            return RemoveVietnameseAccents(text).ToLowerInvariant();
        }
    }
}
