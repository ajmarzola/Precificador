using System.Text.RegularExpressions;

namespace Precificador.Core.Insumos;

public sealed class Insumo
{
    private const int TamanhoMaximoNome = 120;

    private Insumo()
    {
        Nome = null!;
        NomeNormalizado = null!;
    }

    private Insumo(string nome, CategoriaInsumo categoria, UnidadeMedida unidadeBase)
    {
        Nome = NormalizarNome(nome);
        NomeNormalizado = Nome.ToUpperInvariant();
        ValidarNome(Nome);
        ValidarCategoria(categoria);
        ValidarUnidadeBase(unidadeBase);

        Categoria = categoria;
        UnidadeBase = unidadeBase;
        Ativo = true;
    }

    public int Id { get; private set; }

    public int EmpresaId { get; private set; }

    public string Nome { get; private set; }

    public string NomeNormalizado { get; private set; }

    public CategoriaInsumo Categoria { get; private set; }

    public UnidadeMedida UnidadeBase { get; private set; }

    public bool Ativo { get; private set; }

    public static Insumo Criar(string nome, CategoriaInsumo categoria, UnidadeMedida unidadeBase) =>
        new(nome, categoria, unidadeBase);

    public void DefinirEmpresa(int empresaId)
    {
        if (empresaId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(empresaId));
        }

        if (EmpresaId != 0 && EmpresaId != empresaId)
        {
            throw new InvalidOperationException("O insumo já pertence a outra empresa.");
        }

        EmpresaId = empresaId;
    }

    private static string NormalizarNome(string nome) =>
        Regex.Replace(nome?.Trim() ?? string.Empty, @"\s+", " ");

    private static void ValidarNome(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
        {
            throw new ArgumentException("O nome é obrigatório.", nameof(nome));
        }

        if (nome.Length > TamanhoMaximoNome)
        {
            throw new ArgumentException("O nome deve possuir no máximo 120 caracteres.", nameof(nome));
        }
    }

    private static void ValidarCategoria(CategoriaInsumo categoria)
    {
        if (!Enum.IsDefined(categoria))
        {
            throw new ArgumentOutOfRangeException(nameof(categoria), "A categoria informada é inválida.");
        }
    }

    private static void ValidarUnidadeBase(UnidadeMedida unidadeBase)
    {
        if (!Enum.IsDefined(unidadeBase))
        {
            throw new ArgumentOutOfRangeException(nameof(unidadeBase), "A unidade base informada é inválida.");
        }
    }
}
