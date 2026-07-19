using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Precificador.Infrastructure.Importing;

internal static class SpreadsheetValueParser
{
    public static string? AsText(object? value)
    {
        if (value is null || value == DBNull.Value)
            return null;

        var text = value.ToString()?.Trim();
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }

    public static decimal? AsDecimal(object? value)
    {
        if (value is null || value == DBNull.Value)
            return null;

        return value switch
        {
            decimal m => m,
            double d => Convert.ToDecimal(d),
            float f => Convert.ToDecimal(f),
            int i => i,
            long l => l,
            _ => ParseText(value.ToString())
        };
    }

    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        value = value.Trim().ToLowerInvariant();
        value = value.Normalize(NormalizationForm.FormD);

        var chars = value
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            .ToArray();

        value = new string(chars).Normalize(NormalizationForm.FormC);
        return Regex.Replace(value, @"\s+", " ");
    }

    private static decimal? ParseText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        text = text
            .Replace("R$", "", StringComparison.OrdinalIgnoreCase)
            .Replace("%", "", StringComparison.OrdinalIgnoreCase)
            .Trim();

        if (decimal.TryParse(text, NumberStyles.Any, new CultureInfo("pt-BR"), out var ptBr))
            return ptBr;

        if (decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var invariant))
            return invariant;

        return null;
    }
}
