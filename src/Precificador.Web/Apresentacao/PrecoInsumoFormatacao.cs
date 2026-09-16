using System.Globalization;

namespace Precificador.Web.Apresentacao;

public static class PrecoInsumoFormatacao
{
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("pt-BR");

    public static string Data(DateOnly data) => data.ToString("dd/MM/yyyy", Cultura);

    public static string Quantidade(decimal quantidade) => quantidade.ToString("0.######", Cultura);

    public static string MontanteMonetario(decimal valor) => valor.ToString("C2", Cultura);

    public static string CustoUnitarioTecnico(decimal custo) => "R$ " + custo.ToString("0.00####", Cultura);

    public static string PrecoTotal(decimal preco) => MontanteMonetario(preco);

    public static string CustoUnitario(decimal custo) => CustoUnitarioTecnico(custo);

    public static string CustoCalculado(decimal custo) => MontanteMonetario(custo);
}
