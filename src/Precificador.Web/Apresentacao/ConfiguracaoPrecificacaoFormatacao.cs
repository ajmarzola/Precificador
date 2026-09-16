using System.Globalization;

namespace Precificador.Web.Apresentacao;

public static class ConfiguracaoPrecificacaoFormatacao
{
    private const string NaoConfigurado = "Não configurado";
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("pt-BR");

    public static string Monetario(decimal? valor) =>
        valor.HasValue ? PrecoInsumoFormatacao.MontanteMonetario(valor.Value) : NaoConfigurado;

    public static string Numero(decimal? valor) =>
        valor.HasValue ? FormatarDecimal(valor.Value) : NaoConfigurado;

    public static string Percentual(decimal? valor) =>
        valor.HasValue ? Percentual(valor.Value) : NaoConfigurado;

    public static string Percentual(decimal valor) =>
        FormatarDecimal(valor * 100m) + "%";

    private static string FormatarDecimal(decimal valor) =>
        valor.ToString("0.######", Cultura);
}
