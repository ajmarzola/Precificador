namespace Precificador.Core.Precificacao;

public static class CalculadoraCustoProduto
{
    public static ResultadoCalculoCustoProduto Calcular(
        decimal? custoBaseItens,
        decimal? custoPerdasLote,
        decimal? custoMaoDeObraLote,
        decimal? custoEnergiaLote,
        decimal rendimento) => Calcular(custoBaseItens, custoPerdasLote, custoMaoDeObraLote, custoEnergiaLote, 0m, rendimento);

    public static ResultadoCalculoCustoProduto Calcular(
        decimal? custoBaseItens,
        decimal? custoPerdasLote,
        decimal? custoMaoDeObraLote,
        decimal? custoEnergiaLote,
        decimal? custoDesgasteEquipamentosLote,
        decimal rendimento)
    {
        ValidarRendimento(rendimento);
        ValidarCustoConhecido(custoBaseItens, nameof(custoBaseItens));
        ValidarCustoConhecido(custoPerdasLote, nameof(custoPerdasLote));
        ValidarCustoConhecido(custoMaoDeObraLote, nameof(custoMaoDeObraLote));
        ValidarCustoConhecido(custoEnergiaLote, nameof(custoEnergiaLote));
        ValidarCustoConhecido(custoDesgasteEquipamentosLote, nameof(custoDesgasteEquipamentosLote));

        if (custoBaseItens is null || custoPerdasLote is null || custoMaoDeObraLote is null || custoEnergiaLote is null || custoDesgasteEquipamentosLote is null)
        {
            return new ResultadoCalculoCustoProduto(null, null, false);
        }

        var custoLote = custoBaseItens.Value + custoPerdasLote.Value + custoMaoDeObraLote.Value + custoEnergiaLote.Value + custoDesgasteEquipamentosLote.Value;
        return new ResultadoCalculoCustoProduto(custoLote, custoLote / rendimento, true);
    }

    private static void ValidarRendimento(decimal rendimento)
    {
        if (rendimento <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(rendimento));
        }
    }

    private static void ValidarCustoConhecido(decimal? custo, string nome)
    {
        if (custo < 0)
        {
            throw new ArgumentOutOfRangeException(nome);
        }
    }
}

public sealed record ResultadoCalculoCustoProduto(decimal? CustoLote, decimal? CustoUnitarioProduto, bool Completo);
