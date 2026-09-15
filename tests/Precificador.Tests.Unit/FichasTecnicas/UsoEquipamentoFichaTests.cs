using Precificador.Core.FichasTecnicas;

namespace Precificador.Tests.Unit.FichasTecnicas;

public sealed class UsoEquipamentoFichaTests
{
    [Fact]
    public void Criar_NormalizaNomeEPreservaOwnership()
    {
        var uso = UsoEquipamentoFicha.Criar(1, 2, " Forno   elétrico ", 1.25m, 30);
        Assert.Equal("Forno elétrico", uso.NomeEquipamento);
        Assert.Equal("FORNO ELÉTRICO", uso.NomeEquipamentoNormalizado);
        Assert.Equal(1, uso.EmpresaId); Assert.Equal(2, uso.FichaTecnicaId);
    }
    [Theory] [InlineData(0)] [InlineData(-1)]
    public void Criar_RejeitaPotenciaNaoPositiva(decimal potencia) => Assert.Throws<ArgumentOutOfRangeException>(() => UsoEquipamentoFicha.Criar(1, 2, "Forno", potencia, 1));
    [Theory] [InlineData(0)] [InlineData(-1)]
    public void Criar_RejeitaTempoNaoPositivo(int tempo) => Assert.Throws<ArgumentOutOfRangeException>(() => UsoEquipamentoFicha.Criar(1, 2, "Forno", 1m, tempo));
    [Fact]
    public void AtualizarDados_InvalidoNaoMutaEstado()
    {
        var uso = UsoEquipamentoFicha.Criar(1, 2, "Forno", 1m, 20);
        Assert.Throws<ArgumentOutOfRangeException>(() => uso.AtualizarDados("Outro", 0m, 30));
        Assert.Equal("Forno", uso.NomeEquipamento); Assert.Equal(1m, uso.PotenciaKw); Assert.Equal(20, uso.TempoUsoMinutos);
    }
    [Fact] public void DefinirEmpresa_RejeitaReatribuicao() => Assert.Throws<InvalidOperationException>(() => UsoEquipamentoFicha.Criar(1, 2, "Forno", 1m, 1).DefinirEmpresa(2));
}
