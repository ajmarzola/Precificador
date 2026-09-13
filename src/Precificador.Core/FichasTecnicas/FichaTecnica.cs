using Precificador.Core.Empresas;

namespace Precificador.Core.FichasTecnicas;

public sealed class FichaTecnica : IEntidadeEmpresa
{
    private FichaTecnica()
    {
    }

    private FichaTecnica(int empresaId, int produtoId, decimal rendimento, int tempoAtivoMinutos)
    {
        ValidarIds(empresaId, produtoId);
        ValidarBase(rendimento, tempoAtivoMinutos);

        EmpresaId = empresaId;
        ProdutoId = produtoId;
        Rendimento = rendimento;
        TempoAtivoMinutos = tempoAtivoMinutos;
    }

    public int Id { get; private set; }

    public int EmpresaId { get; private set; }

    public int ProdutoId { get; private set; }

    public decimal Rendimento { get; private set; }

    public int TempoAtivoMinutos { get; private set; }

    public static FichaTecnica Criar(int empresaId, int produtoId, decimal rendimento, int tempoAtivoMinutos) =>
        new(empresaId, produtoId, rendimento, tempoAtivoMinutos);

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

    public void AtualizarBase(decimal rendimento, int tempoAtivoMinutos)
    {
        ValidarBase(rendimento, tempoAtivoMinutos);

        Rendimento = rendimento;
        TempoAtivoMinutos = tempoAtivoMinutos;
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

    private static void ValidarBase(decimal rendimento, int tempoAtivoMinutos)
    {
        if (rendimento <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(rendimento), "O rendimento deve ser maior que zero.");
        }

        if (tempoAtivoMinutos < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tempoAtivoMinutos), "O tempo ativo não pode ser negativo.");
        }
    }
}
