namespace Precificador.Core.Precificacao;

public static class CalculadoraCustoPerdas
{
    public static ResultadoCalculoCustoPerdas Calcular(IEnumerable<ItemCustoPerdaEntrada> itens)
    {
        var resultados = itens.Select(item =>
        {
            ValidarPercentualPerda(item.PercentualPerda);
            var custoPerda = item.PercentualPerda == 0m
                ? 0m
                : item.CustoItemBase * item.PercentualPerda;
            return new ItemCustoPerdaCalculado(item.ItemId, item.PercentualPerda, custoPerda);
        }).ToList();

        var completo = resultados.All(item => item.CustoPerdaItem is not null);
        decimal? custoPerdasLote = completo ? resultados.Sum(item => item.CustoPerdaItem!.Value) : null;
        return new ResultadoCalculoCustoPerdas(resultados, custoPerdasLote, completo);
    }

    private static void ValidarPercentualPerda(decimal percentualPerda)
    {
        if (percentualPerda < 0 || percentualPerda >= 1)
        {
            throw new ArgumentOutOfRangeException(nameof(percentualPerda));
        }
    }
}

public sealed record ItemCustoPerdaEntrada(int ItemId, decimal PercentualPerda, decimal? CustoItemBase);

public sealed record ItemCustoPerdaCalculado(int ItemId, decimal PercentualPerda, decimal? CustoPerdaItem);

public sealed record ResultadoCalculoCustoPerdas(
    IReadOnlyList<ItemCustoPerdaCalculado> Itens,
    decimal? CustoPerdasLote,
    bool Completo);
