namespace Precificador.Core.Precificacao;

public static class CalculadoraMargemAtual
{
    public static ResultadoCalculoMargemAtual Calcular(
        decimal? custoUnitarioProduto,
        decimal? precoPrateleiraAtual,
        decimal margemAlvo)
    {
        if (margemAlvo < 0 || margemAlvo >= 1) throw new ArgumentOutOfRangeException(nameof(margemAlvo));
        if (custoUnitarioProduto < 0) throw new ArgumentOutOfRangeException(nameof(custoUnitarioProduto));
        if (precoPrateleiraAtual <= 0) throw new ArgumentOutOfRangeException(nameof(precoPrateleiraAtual));

        if (custoUnitarioProduto is null || precoPrateleiraAtual is null)
        {
            return new ResultadoCalculoMargemAtual(null, SituacaoMargemProduto.Incompleto);
        }

        var margemAtual = (precoPrateleiraAtual.Value - custoUnitarioProduto.Value) / precoPrateleiraAtual.Value;
        var situacao = margemAtual < margemAlvo
            ? SituacaoMargemProduto.AbaixoDaMargem
            : SituacaoMargemProduto.DentroDaMargem;

        return new ResultadoCalculoMargemAtual(margemAtual, situacao);
    }
}

public sealed record ResultadoCalculoMargemAtual(decimal? MargemAtual, SituacaoMargemProduto Situacao);

public enum SituacaoMargemProduto
{
    Incompleto = 0,
    AbaixoDaMargem = 1,
    DentroDaMargem = 2
}
