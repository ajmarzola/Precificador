namespace Precificador.Core.Precificacao;

public static class CalculadoraPrecoProduto
{
    public static ResultadoCalculoPrecoProduto Calcular(decimal? custoUnitarioProduto, decimal margemAlvo, decimal? incrementoComercial)
    {
        if (margemAlvo < 0 || margemAlvo >= 1) throw new ArgumentOutOfRangeException(nameof(margemAlvo));
        if (custoUnitarioProduto < 0) throw new ArgumentOutOfRangeException(nameof(custoUnitarioProduto));
        if (incrementoComercial <= 0) throw new ArgumentOutOfRangeException(nameof(incrementoComercial));

        if (custoUnitarioProduto is null) return new(null, null, false);

        var precoTeorico = custoUnitarioProduto.Value / (1m - margemAlvo);
        if (incrementoComercial is null) return new(precoTeorico, null, false);

        var precoSugerido = decimal.Ceiling(precoTeorico / incrementoComercial.Value) * incrementoComercial.Value;
        return new(precoTeorico, precoSugerido, true);
    }
}

public sealed record ResultadoCalculoPrecoProduto(decimal? PrecoTeorico, decimal? PrecoSugerido, bool Completo);
