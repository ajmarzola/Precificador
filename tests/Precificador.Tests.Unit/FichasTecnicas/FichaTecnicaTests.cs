using Precificador.Core.FichasTecnicas;

namespace Precificador.Tests.Unit.FichasTecnicas;

public sealed class FichaTecnicaTests
{
    [Fact]
    public void CA03_Criar_ficha_valida_preserva_empresa_produto_rendimento_e_tempo()
    {
        var ficha = FichaTecnica.Criar(7, 11, 2.5m, 45);

        Assert.Equal(7, ficha.EmpresaId);
        Assert.Equal(11, ficha.ProdutoId);
        Assert.Equal(2.5m, ficha.Rendimento);
        Assert.Equal(45, ficha.TempoAtivoMinutos);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-0.01")]
    public void CA05_Rendimento_deve_ser_maior_que_zero(string rendimento)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            FichaTecnica.Criar(1, 1, decimal.Parse(rendimento, System.Globalization.CultureInfo.InvariantCulture), 10));
    }

    [Fact]
    public void CA06_Tempo_ativo_aceita_zero_e_rejeita_negativo()
    {
        var ficha = FichaTecnica.Criar(1, 1, 1m, 0);

        Assert.Equal(0, ficha.TempoAtivoMinutos);
        Assert.Throws<ArgumentOutOfRangeException>(() => FichaTecnica.Criar(1, 1, 1m, -1));
    }

    [Fact]
    public void CA08_Atualizar_base_preserva_id_empresa_e_produto()
    {
        var ficha = FichaTecnica.Criar(7, 11, 2m, 30);

        ficha.AtualizarBase(3.5m, 60);

        Assert.Equal(0, ficha.Id);
        Assert.Equal(7, ficha.EmpresaId);
        Assert.Equal(11, ficha.ProdutoId);
        Assert.Equal(3.5m, ficha.Rendimento);
        Assert.Equal(60, ficha.TempoAtivoMinutos);
    }

    [Fact]
    public void CA09_Atualizacao_invalida_nao_altera_estado_anterior()
    {
        var ficha = FichaTecnica.Criar(7, 11, 2m, 30);

        Assert.Throws<ArgumentOutOfRangeException>(() => ficha.AtualizarBase(0m, -1));

        Assert.Equal(7, ficha.EmpresaId);
        Assert.Equal(11, ficha.ProdutoId);
        Assert.Equal(2m, ficha.Rendimento);
        Assert.Equal(30, ficha.TempoAtivoMinutos);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-1, 1)]
    [InlineData(1, 0)]
    [InlineData(1, -1)]
    public void CA03_Ids_de_empresa_e_produto_devem_ser_positivos(int empresaId, int produtoId)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => FichaTecnica.Criar(empresaId, produtoId, 1m, 10));
    }
}
