using Precificador.Core.Empresas;

namespace Precificador.Core.FichasTecnicas;

public sealed class FichaTecnica : IEntidadeEmpresa
{
    private FichaTecnica()
    {
    }

    private FichaTecnica(int empresaId, int produtoId, decimal rendimento)
    {
        ValidarIds(empresaId, produtoId);
        ValidarBase(rendimento);

        EmpresaId = empresaId;
        ProdutoId = produtoId;
        Rendimento = rendimento;
    }

    public int Id { get; private set; }

    public int EmpresaId { get; private set; }

    public int ProdutoId { get; private set; }

    public decimal Rendimento { get; private set; }

    public static FichaTecnica Criar(int empresaId, int produtoId, decimal rendimento) =>
        new(empresaId, produtoId, rendimento);

    public void DefinirEmpresa(int empresaId)
    {
        if (empresaId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(empresaId));
        }

        if (EmpresaId != 0 && EmpresaId != empresaId)
        {
            throw new InvalidOperationException("A ficha técnica já pertence a outra empresa.");
        }

        EmpresaId = empresaId;
    }

    public void AtualizarBase(decimal rendimento)
    {
        ValidarBase(rendimento);

        Rendimento = rendimento;
    }

    private static void ValidarIds(int empresaId, int produtoId)
    {
        if (empresaId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(empresaId));
        }

        if (produtoId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(produtoId));
        }
    }

    private static void ValidarBase(decimal rendimento)
    {
        if (rendimento <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(rendimento), "O rendimento deve ser maior que zero.");
        }

    }
}
