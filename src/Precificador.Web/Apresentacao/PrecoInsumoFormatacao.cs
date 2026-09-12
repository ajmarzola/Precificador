using System.Globalization;

namespace Precificador.Web.Apresentacao;

public static class PrecoInsumoFormatacao
{
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("pt-BR");

    public static string Data(DateOnly data) => data.ToString("dd/MM/yyyy", Cultura);

    public static string Quantidade(decimal quantidade) => quantidade.ToString("0.######", Cultura);

    public static string PrecoTotal(decimal preco) => preco.ToString("0.####", Cultura);

    public static string CustoUnitario(decimal custo) => custo.ToString("0.######", Cultura);
}
