using System.Globalization;

namespace Precificador.Web.Apresentacao;

public static class ProdutoFormatacao
{
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("pt-BR");

    public static string MargemAlvo(decimal margemAlvo) =>
        (margemAlvo * 100m).ToString("0.##", Cultura) + "%";
}
