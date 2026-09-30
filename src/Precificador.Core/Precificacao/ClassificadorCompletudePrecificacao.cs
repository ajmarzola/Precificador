namespace Precificador.Core.Precificacao;

public enum MotivoPrecificacaoIncompleta
{
    FichaTecnicaAusente,
    FichaTecnicaSemItens,
    InsumoSemPrecoVigente,
    ConfiguracaoPrecificacaoAusente,
    TarifaEnergiaNaoConfigurada,
    IncrementoComercialNaoConfigurado,
    PrecoPrateleiraNaoDefinido
}

public sealed record EntradaCompletudePrecificacao(
    bool FichaTecnicaExiste,
    bool FichaTecnicaTemItens,
    bool HaInsumoSemPrecoVigente,
    bool ConfiguracaoPrecificacaoExiste,
    bool HaUsoEquipamentoEletrico,
    decimal? TarifaEnergiaKwh,
    decimal? IncrementoComercial,
    bool CustoProdutoCompleto,
    bool PrecoProdutoCompleto,
    decimal? PrecoPrateleiraAtual,
    decimal? MargemAtual);

public sealed record ResultadoCompletudePrecificacao(
    bool PrecificacaoCompleta,
    IReadOnlyList<MotivoPrecificacaoIncompleta> Motivos);

/// <summary>Classifica somente fatos já obtidos pela orquestração, sem consultar persistência.</summary>
public static class ClassificadorCompletudePrecificacao
{
    public static ResultadoCompletudePrecificacao Classificar(EntradaCompletudePrecificacao entrada)
    {
        var motivos = new List<MotivoPrecificacaoIncompleta>();

        if (!entrada.FichaTecnicaExiste)
        {
            motivos.Add(MotivoPrecificacaoIncompleta.FichaTecnicaAusente);
        }
        else
        {
            if (!entrada.FichaTecnicaTemItens) motivos.Add(MotivoPrecificacaoIncompleta.FichaTecnicaSemItens);
            if (entrada.HaInsumoSemPrecoVigente) motivos.Add(MotivoPrecificacaoIncompleta.InsumoSemPrecoVigente);
        }

        if (!entrada.ConfiguracaoPrecificacaoExiste)
        {
            motivos.Add(MotivoPrecificacaoIncompleta.ConfiguracaoPrecificacaoAusente);
        }
        else
        {
            if (entrada.HaUsoEquipamentoEletrico && entrada.TarifaEnergiaKwh is null)
                motivos.Add(MotivoPrecificacaoIncompleta.TarifaEnergiaNaoConfigurada);
            if (entrada.IncrementoComercial is null)
                motivos.Add(MotivoPrecificacaoIncompleta.IncrementoComercialNaoConfigurado);
        }

        if (entrada.PrecoPrateleiraAtual is null) motivos.Add(MotivoPrecificacaoIncompleta.PrecoPrateleiraNaoDefinido);

        var completa = motivos.Count == 0
            && entrada.CustoProdutoCompleto
            && entrada.PrecoProdutoCompleto
            && entrada.PrecoPrateleiraAtual is not null
            && entrada.MargemAtual is not null;

        return new ResultadoCompletudePrecificacao(completa, motivos);
    }
}
