using Precificador.Core.Empresas;

namespace Precificador.Core.Produtos;

public sealed class ColecaoProdutoCategoria : IEntidadeEmpresa
{
    private ColecaoProdutoCategoria() { }

    public ColecaoProdutoCategoria(ColecaoProduto colecao, int categoriaProdutoId)
    {
        ColecaoProduto = colecao ?? throw new ArgumentNullException(nameof(colecao));
        EmpresaId = colecao.EmpresaId;
        CategoriaProdutoId = categoriaProdutoId;
    }

    public int EmpresaId { get; private set; }
    public int ColecaoProdutoId { get; private set; }
    public int CategoriaProdutoId { get; private set; }
    public ColecaoProduto ColecaoProduto { get; private set; } = null!;
    public CategoriaProduto CategoriaProduto { get; private set; } = null!;

    public void DefinirEmpresa(int empresaId)
    {
        if (empresaId <= 0) throw new ArgumentOutOfRangeException(nameof(empresaId));
        if (EmpresaId != 0 && EmpresaId != empresaId) throw new InvalidOperationException("A associação já pertence a outra empresa.");
        EmpresaId = empresaId;
    }
}
