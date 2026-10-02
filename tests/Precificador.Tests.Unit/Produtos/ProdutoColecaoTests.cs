using Precificador.Core.Produtos;

namespace Precificador.Tests.Unit.Produtos;

public sealed class ProdutoColecaoTests
{
    [Theory]
    [InlineData(0, 1, 1)] [InlineData(-1, 1, 1)]
    [InlineData(1, 0, 1)] [InlineData(1, -1, 1)]
    [InlineData(1, 1, 0)] [InlineData(1, 1, -1)]
    public void Rejeita_ids_invalidos(int empresa, int produto, int colecao) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => ProdutoColecao.Criar(empresa, produto, colecao));

    [Fact]
    public void Destaque_e_por_vinculo_sem_exclusividade_e_empresa_nao_muda()
    {
        var primeiro = ProdutoColecao.Criar(1, 2, 3);
        var segundo = ProdutoColecao.Criar(1, 2, 4, true);
        var terceiro = ProdutoColecao.Criar(1, 5, 3, true);
        Assert.False(primeiro.Destaque);
        primeiro.DefinirDestaque(true);
        Assert.All(new[] { primeiro, segundo, terceiro }, x => Assert.True(x.Destaque));
        primeiro.DefinirDestaque(false);
        Assert.False(primeiro.Destaque);
        Assert.True(segundo.Destaque);
        Assert.True(terceiro.Destaque);
        primeiro.DefinirEmpresa(1);
        Assert.Throws<InvalidOperationException>(() => primeiro.DefinirEmpresa(2));
        Assert.Throws<ArgumentOutOfRangeException>(() => primeiro.DefinirEmpresa(0));
        Assert.Equal(1, primeiro.EmpresaId);
        Assert.Equal(2, primeiro.ProdutoId);
        Assert.Equal(3, primeiro.ColecaoProdutoId);
    }
}
