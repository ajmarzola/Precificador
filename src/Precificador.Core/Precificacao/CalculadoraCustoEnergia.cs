namespace Precificador.Core.Precificacao;

public static class CalculadoraCustoEnergia
{
    public static ResultadoCalculoCustoEnergia Calcular(decimal? tarifaEnergiaKwh, IEnumerable<UsoEquipamentoCustoEntrada> usos)
    {
        if (tarifaEnergiaKwh < 0) throw new ArgumentOutOfRangeException(nameof(tarifaEnergiaKwh));
        var usosCalculados = usos.Select(uso => CalcularUso(uso, tarifaEnergiaKwh)).ToList();
        if (usosCalculados.Count == 0) return new ResultadoCalculoCustoEnergia(usosCalculados, 0m, true);
        if (tarifaEnergiaKwh is null) return new ResultadoCalculoCustoEnergia(usosCalculados, null, false);
        return new ResultadoCalculoCustoEnergia(usosCalculados, usosCalculados.Sum(uso => uso.CustoEnergiaUso!.Value), true);
    }

    private static UsoEquipamentoCustoResultado CalcularUso(UsoEquipamentoCustoEntrada uso, decimal? tarifa)
    {
        if (uso.PotenciaKw <= 0) throw new ArgumentOutOfRangeException(nameof(uso.PotenciaKw));
        if (uso.TempoUsoMinutos <= 0) throw new ArgumentOutOfRangeException(nameof(uso.TempoUsoMinutos));
        var consumo = uso.PotenciaKw * (uso.TempoUsoMinutos / 60m);
        return new UsoEquipamentoCustoResultado(uso.UsoId, consumo, tarifa is null ? null : consumo * tarifa.Value);
    }
}

public sealed record UsoEquipamentoCustoEntrada(int UsoId, decimal PotenciaKw, int TempoUsoMinutos);
public sealed record UsoEquipamentoCustoResultado(int UsoId, decimal ConsumoKwh, decimal? CustoEnergiaUso);
public sealed record ResultadoCalculoCustoEnergia(IReadOnlyList<UsoEquipamentoCustoResultado> Usos, decimal? CustoEnergiaLote, bool Completo);
