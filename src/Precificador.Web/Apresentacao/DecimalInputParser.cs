using System.Globalization;
using System.Text.RegularExpressions;

namespace Precificador.Web.Apresentacao;

public static partial class DecimalInputParser
{
    private static readonly NumberStyles Estilo = NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint;

    [GeneratedRegex(@"^[+-]?\d+(?:[\.,]\d+)?$", RegexOptions.CultureInvariant)]
    private static partial Regex FormatoDecimalSimples();

    public static bool TentarParse(string? texto, out decimal valor)
    {
        valor = 0m;
        var valorInformado = texto?.Trim();
        if (string.IsNullOrEmpty(valorInformado))
        {
            return false;
        }

        if (!FormatoDecimalSimples().IsMatch(valorInformado))
        {
            return false;
        }

        if (valorInformado.Contains(',') && valorInformado.Contains('.'))
        {
            return false;
        }

        var normalizado = valorInformado.Replace(',', '.');
        return decimal.TryParse(normalizado, Estilo, CultureInfo.InvariantCulture, out valor);
    }
}