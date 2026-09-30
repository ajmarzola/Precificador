using Precificador.Core.Precificacao;

namespace Precificador.Tests.Unit.Precificacao;

public sealed class ClassificadorCompletudePrecificacaoTests
{
    [Fact]
    public void Tudo_disponivel_resulta_em_precificacao_completa()
    {
        var resultado = Classificar();

        Assert.True(resultado.PrecificacaoCompleta);
        Assert.Empty(resultado.Motivos);
    }

    [Theory]
    [InlineData(nameof(MotivoPrecificacaoIncompleta.FichaTecnicaAusente))]
    [InlineData(nameof(MotivoPrecificacaoIncompleta.FichaTecnicaSemItens))]
    [InlineData(nameof(MotivoPrecificacaoIncompleta.InsumoSemPrecoVigente))]
    [InlineData(nameof(MotivoPrecificacaoIncompleta.ConfiguracaoPrecificacaoAusente))]
    [InlineData(nameof(MotivoPrecificacaoIncompleta.IncrementoComercialNaoConfigurado))]
    [InlineData(nameof(MotivoPrecificacaoIncompleta.PrecoPrateleiraNaoDefinido))]
    public void Ausencias_independentes_sao_motivos_estruturados(string motivo)
    {
        var entrada = Completa() with
        {
            FichaTecnicaExiste = motivo != nameof(MotivoPrecificacaoIncompleta.FichaTecnicaAusente),
            FichaTecnicaTemItens = motivo != nameof(MotivoPrecificacaoIncompleta.FichaTecnicaSemItens),
            HaInsumoSemPrecoVigente = motivo == nameof(MotivoPrecificacaoIncompleta.InsumoSemPrecoVigente),
            ConfiguracaoPrecificacaoExiste = motivo != nameof(MotivoPrecificacaoIncompleta.ConfiguracaoPrecificacaoAusente),
            IncrementoComercial = motivo == nameof(MotivoPrecificacaoIncompleta.IncrementoComercialNaoConfigurado) ? null : .50m,
            PrecoPrateleiraAtual = motivo == nameof(MotivoPrecificacaoIncompleta.PrecoPrateleiraNaoDefinido) ? null : 20m
        };

        var resultado = ClassificadorCompletudePrecificacao.Classificar(entrada);

        Assert.False(resultado.PrecificacaoCompleta);
        Assert.Contains(Enum.Parse<MotivoPrecificacaoIncompleta>(motivo), resultado.Motivos);
    }

    [Fact]
    public void Tarifa_nula_so_e_motivo_com_uso_eletrico()
    {
        var semUso = Classificar(Completa() with { TarifaEnergiaKwh = null });
        var comUso = Classificar(Completa() with { HaUsoEquipamentoEletrico = true, TarifaEnergiaKwh = null, CustoProdutoCompleto = false, PrecoProdutoCompleto = false, MargemAtual = null });

        Assert.True(semUso.PrecificacaoCompleta);
        Assert.DoesNotContain(MotivoPrecificacaoIncompleta.TarifaEnergiaNaoConfigurada, semUso.Motivos);
        Assert.Contains(MotivoPrecificacaoIncompleta.TarifaEnergiaNaoConfigurada, comUso.Motivos);
    }

    [Fact]
    public void Configuracao_ausente_nao_inventa_motivos_de_tarifa_ou_incremento()
    {
        var resultado = Classificar(Completa() with { ConfiguracaoPrecificacaoExiste = false, TarifaEnergiaKwh = null, IncrementoComercial = null, HaUsoEquipamentoEletrico = true, CustoProdutoCompleto = false, PrecoProdutoCompleto = false, MargemAtual = null });

        Assert.Equal([MotivoPrecificacaoIncompleta.ConfiguracaoPrecificacaoAusente], resultado.Motivos);
    }

    [Fact]
    public void Motivos_multiplos_sao_deduplicados_e_tem_ordem_estavel()
    {
        var resultado = Classificar(Completa() with
        {
            FichaTecnicaTemItens = false,
            HaInsumoSemPrecoVigente = true,
            HaUsoEquipamentoEletrico = true,
            TarifaEnergiaKwh = null,
            IncrementoComercial = null,
            PrecoPrateleiraAtual = null,
            CustoProdutoCompleto = false,
            PrecoProdutoCompleto = false,
            MargemAtual = null
        });

        Assert.Equal([
            MotivoPrecificacaoIncompleta.FichaTecnicaSemItens,
            MotivoPrecificacaoIncompleta.InsumoSemPrecoVigente,
            MotivoPrecificacaoIncompleta.TarifaEnergiaNaoConfigurada,
            MotivoPrecificacaoIncompleta.IncrementoComercialNaoConfigurado,
            MotivoPrecificacaoIncompleta.PrecoPrateleiraNaoDefinido], resultado.Motivos);
    }

    private static ResultadoCompletudePrecificacao Classificar(EntradaCompletudePrecificacao? entrada = null) =>
        ClassificadorCompletudePrecificacao.Classificar(entrada ?? Completa());

    private static EntradaCompletudePrecificacao Completa() => new(true, true, false, true, false, 0m, .50m, true, true, 20m, .50m);
}
