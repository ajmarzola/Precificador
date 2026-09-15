namespace Precificador.Core.Precificacao;

public static class CalculadoraDescontoReferencia
{
    private const decimal PontoPercentualMinimo = 0.01m;

    public static decimal? Calcular(
        decimal precoSugerido,
        decimal precoPrateleira,
        decimal reservaComercialReferencia)
    {
        if (precoSugerido < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(precoSugerido));
        }

        if (precoPrateleira <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(precoPrateleira));
        }

        if (reservaComercialReferencia < 0 || reservaComercialReferencia >= 1)
        {
            throw new ArgumentOutOfRangeException(nameof(reservaComercialReferencia));
        }

        if (precoSugerido == 0 || precoPrateleira < precoSugerido)
        {
            return null;
        }

        var percentualAcimaSugerido = (precoPrateleira / precoSugerido) - 1;
        var limiarAplicacao = reservaComercialReferencia + PontoPercentualMinimo;

        return percentualAcimaSugerido < limiarAplicacao
            ? null
            : percentualAcimaSugerido - reservaComercialReferencia;
    }
}
