namespace Precificador.Core.Precificacao;

public static class CalculadoraCustoMaoDeObra
{
    public static ResultadoCalculoCustoMaoDeObra Calcular(decimal? custoBaseItens, decimal percentualMaoDeObra)
    {
        if (custoBaseItens < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(custoBaseItens));
        }

        if (percentualMaoDeObra < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(percentualMaoDeObra));
        }

        if (custoBaseItens is null)
        {
            return new ResultadoCalculoCustoMaoDeObra(null, false);
        }

        return new ResultadoCalculoCustoMaoDeObra(
            custoBaseItens.Value * percentualMaoDeObra,
            true);
    }
}

public sealed record ResultadoCalculoCustoMaoDeObra(decimal? CustoMaoDeObraLote, bool Completo);
