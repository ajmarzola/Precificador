using Precificador.Core.Empresas;

namespace Precificador.Core.Produtos;

public sealed class ProdutoColecao : IEntidadeEmpresa
{
    private ProdutoColecao() { }
    public int EmpresaId { get; private set; }
    public int ProdutoId { get; private set; }
    public int ColecaoProdutoId { get; private set; }
    public bool Destaque { get; private set; }
    public Produto Produto { get; private set; } = null!;
    public ColecaoProduto ColecaoProduto { get; private set; } = null!;

    public static ProdutoColecao Criar(int empresaId, int produtoId, int colecaoProdutoId, bool destaque = false)
    {
        if (produtoId <= 0) throw new ArgumentOutOfRangeException(nameof(produtoId));
        if (colecaoProdutoId <= 0) throw new ArgumentOutOfRangeException(nameof(colecaoProdutoId));
        var vinculo = new ProdutoColecao { ProdutoId = produtoId, ColecaoProdutoId = colecaoProdutoId, Destaque = destaque };
        vinculo.DefinirEmpresa(empresaId);
        return vinculo;
    }

    public void DefinirDestaque(bool destaque) => Destaque = destaque;

    public void DefinirEmpresa(int empresaId)
    {
        if (empresaId <= 0) throw new ArgumentOutOfRangeException(nameof(empresaId));
        if (EmpresaId != 0 && EmpresaId != empresaId) throw new InvalidOperationException("O vínculo já pertence a outra empresa.");
        EmpresaId = empresaId;
    }
}
