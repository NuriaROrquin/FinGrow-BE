namespace FinGrow.Application.Features.SecurityPrices;

using System.Globalization;
using System.Text;

internal static class SearchText
{
    public static string Normalize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var decomposed = text.Trim().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        var previousWasSpace = false;

        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            var isSpace = char.IsWhiteSpace(character);

            if (!isSpace || !previousWasSpace)
            {
                builder.Append(isSpace ? ' ' : char.ToUpperInvariant(character));
            }

            previousWasSpace = isSpace;
        }

        return builder.ToString();
    }
}
