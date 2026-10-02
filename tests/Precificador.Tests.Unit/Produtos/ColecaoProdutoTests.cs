using Precificador.Core.Produtos;

namespace Precificador.Tests.Unit.Produtos;

public sealed class ColecaoProdutoTests
{
    [Fact]
    public void Nome_periodo_e_situacao_respeitam_bordas()
    {
        var inicio = new DateOnly(2027, 12, 1);
        var fim = new DateOnly(2027, 12, 24);
        var colecao = ColecaoProduto.Criar(1, "  Natal   Especial  ", inicio, fim);
        Assert.Equal("Natal Especial", colecao.Nome);
        Assert.Equal("NATAL ESPECIAL", colecao.NomeNormalizado);
        Assert.Equal(SituacaoColecaoProduto.Planejada, colecao.ObterSituacao(inicio.AddDays(-1)));
        Assert.Equal(SituacaoColecaoProduto.EmAndamento, colecao.ObterSituacao(inicio));
        Assert.Equal(SituacaoColecaoProduto.EmAndamento, colecao.ObterSituacao(fim));
        Assert.Equal(SituacaoColecaoProduto.Finalizada, colecao.ObterSituacao(fim.AddDays(1)));
        colecao.AtualizarDados("Natal", inicio, null);
        Assert.Equal(SituacaoColecaoProduto.EmAndamento, colecao.ObterSituacao(fim.AddYears(2)));
    }

    [Fact]
    public void Atualizacao_invalida_e_atomica()
    {
        var inicio = new DateOnly(2027, 1, 1);
        var colecao = ColecaoProduto.Criar(1, "Original", inicio, null);
        Assert.Throws<ArgumentException>(() => colecao.AtualizarDados("Mudado", inicio, inicio.AddDays(-1)));
        Assert.Equal("Original", colecao.Nome);
        Assert.Null(colecao.DataFinalizacao);
        Assert.Throws<ArgumentException>(() => colecao.AtualizarDados(new string('X', 121), inicio, null));
        Assert.Throws<ArgumentException>(() => colecao.AtualizarDados("Novo", default, null));
        Assert.Throws<ArgumentException>(() => colecao.AtualizarDados("  ", inicio, null));
        Assert.Throws<InvalidOperationException>(() => colecao.DefinirEmpresa(2));
        Assert.Equal(1, colecao.EmpresaId);
        colecao.AtualizarDados("Um dia", inicio, inicio);
        Assert.Equal(SituacaoColecaoProduto.Finalizada, colecao.ObterSituacao(inicio.AddDays(1)));
    }
}
