using Precificador.Core.Produtos;

namespace Precificador.Core.Precificacao;

public static class CalculadoraCustoDesgasteEquipamentos
{
    public static ResultadoCalculoCustoDesgasteEquipamentos Calcular(
        FormaCalculoDesgasteEquipamento? forma,
        decimal? valor,
        decimal? custoBaseItens)
    {
        if (forma is null)
            return new ResultadoCalculoCustoDesgasteEquipamentos(0m, true);

        Validar(forma.Value, valor ?? throw new ArgumentNullException(nameof(valor)));
        if (forma == FormaCalculoDesgasteEquipamento.ValorFixoPorLote)
            return new ResultadoCalculoCustoDesgasteEquipamentos(valor, true);

        if (valor == 0m)
            return new ResultadoCalculoCustoDesgasteEquipamentos(0m, true);

        return custoBaseItens is null
            ? new ResultadoCalculoCustoDesgasteEquipamentos(null, false)
            : new ResultadoCalculoCustoDesgasteEquipamentos(custoBaseItens.Value * valor.Value, true);
    }

    public static void Validar(FormaCalculoDesgasteEquipamento forma, decimal valor)
    {
        if (!Enum.IsDefined(forma)) throw new ArgumentOutOfRangeException(nameof(forma));
        if (valor < 0m) throw new ArgumentOutOfRangeException(nameof(valor));
    }
}

public sealed record ResultadoCalculoCustoDesgasteEquipamentos(decimal? CustoDesgasteEquipamentosLote, bool Completo);
