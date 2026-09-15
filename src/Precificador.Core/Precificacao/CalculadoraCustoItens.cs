namespace Precificador.Core.Precificacao;

public static class CalculadoraCustoItens
{
    public static ResultadoCalculoCustoItens Calcular(IEnumerable<ItemCustoEntrada> itens)
    {
        var resultados = itens
            .Select(item => new ItemCustoCalculado(
                item.ItemId,
                item.CustoUnitario,
                item.CustoUnitario is null ? null : item.Quantidade * item.CustoUnitario.Value))
            .ToList();

        var completo = resultados.Count > 0 && resultados.All(item => item.CustoUnitario is not null);
        decimal? custoBaseItens = completo
            ? resultados.Sum(item => item.CustoItem!.Value)
            : null;

        return new ResultadoCalculoCustoItens(resultados, custoBaseItens, completo);
    }
}

public sealed record ItemCustoEntrada(int ItemId, decimal Quantidade, decimal? CustoUnitario);

public sealed record ItemCustoCalculado(int ItemId, decimal? CustoUnitario, decimal? CustoItem);

public sealed record ResultadoCalculoCustoItens(
    IReadOnlyList<ItemCustoCalculado> Itens,
    decimal? CustoBaseItens,
    bool Completo);
