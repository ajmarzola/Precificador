using Precificador.Core.FichasTecnicas;

namespace Precificador.Tests.Unit.FichasTecnicas;

public sealed class FichaTecnicaTests
{
    [Fact]
    public void CA03_Criar_ficha_valida_preserva_empresa_produto_e_rendimento()
    {
        var ficha = FichaTecnica.Criar(7, 11, 2.5m);

        Assert.Equal(7, ficha.EmpresaId);
        Assert.Equal(11, ficha.ProdutoId);
        Assert.Equal(2.5m, ficha.Rendimento);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-0.01")]
    public void CA05_Rendimento_deve_ser_maior_que_zero(string rendimento)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            FichaTecnica.Criar(1, 1, decimal.Parse(rendimento, System.Globalization.CultureInfo.InvariantCulture)));
    }

    [Fact]
    public void CA08_Atualizar_base_preserva_id_empresa_e_produto()
    {
        var ficha = FichaTecnica.Criar(7, 11, 2m);

        ficha.AtualizarBase(3.5m);

        Assert.Equal(0, ficha.Id);
        Assert.Equal(7, ficha.EmpresaId);
        Assert.Equal(11, ficha.ProdutoId);
        Assert.Equal(3.5m, ficha.Rendimento);
    }

    [Fact]
    public void CA09_Atualizacao_invalida_nao_altera_estado_anterior()
    {
        var ficha = FichaTecnica.Criar(7, 11, 2m);

        Assert.Throws<ArgumentOutOfRangeException>(() => ficha.AtualizarBase(0m));

        Assert.Equal(7, ficha.EmpresaId);
        Assert.Equal(11, ficha.ProdutoId);
        Assert.Equal(2m, ficha.Rendimento);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-1, 1)]
    [InlineData(1, 0)]
    [InlineData(1, -1)]
    public void CA03_Ids_de_empresa_e_produto_devem_ser_positivos(int empresaId, int produtoId)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => FichaTecnica.Criar(empresaId, produtoId, 1m));
    }
}
