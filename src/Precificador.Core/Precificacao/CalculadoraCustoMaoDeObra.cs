namespace Precificador.Core.Precificacao;

public static class CalculadoraCustoMaoDeObra
{
    public static ResultadoCalculoCustoMaoDeObra Calcular(int tempoAtivoMinutos, decimal? valorHoraTrabalho)
    {
        if (tempoAtivoMinutos < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tempoAtivoMinutos));
        }

        if (valorHoraTrabalho < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(valorHoraTrabalho));
        }

        if (tempoAtivoMinutos == 0)
        {
            return new ResultadoCalculoCustoMaoDeObra(0m, true);
        }

        if (valorHoraTrabalho is null)
        {
            return new ResultadoCalculoCustoMaoDeObra(null, false);
        }

        return new ResultadoCalculoCustoMaoDeObra(
            (tempoAtivoMinutos / 60m) * valorHoraTrabalho.Value,
            true);
    }
}

public sealed record ResultadoCalculoCustoMaoDeObra(decimal? CustoMaoDeObraLote, bool Completo);
