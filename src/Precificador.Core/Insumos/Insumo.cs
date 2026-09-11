using System.Text.RegularExpressions;

namespace Precificador.Core.Insumos;

public sealed class Insumo : Precificador.Core.Empresas.IEntidadeEmpresa
{
    private const int TamanhoMaximoNome = 120;
    private const int TamanhoMaximoMarca = 80;
    private const int TamanhoMaximoObservacao = 1000;

    private Insumo()
    {
        Nome = null!;
        NomeNormalizado = null!;
        MarcaNormalizada = null!;
    }

    private Insumo(int empresaId, string nome, CategoriaInsumo categoria, UnidadeMedida unidadeBase, string? marca, string? observacao)
    {
        DefinirEmpresa(empresaId);
        Nome = NormalizarNome(nome);
        NomeNormalizado = Nome.ToUpperInvariant();
        Marca = NormalizarMarca(marca);
        MarcaNormalizada = Marca?.ToUpperInvariant() ?? string.Empty;
        Observacao = NormalizarObservacao(observacao);
        ValidarNome(Nome);
        ValidarMarca(Marca);
        ValidarObservacao(Observacao);
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

    public string? Marca { get; private set; }

    public string MarcaNormalizada { get; private set; }

    public string? Observacao { get; private set; }

    public CategoriaInsumo Categoria { get; private set; }

    public UnidadeMedida UnidadeBase { get; private set; }

    public bool Ativo { get; private set; }

    public static Insumo Criar(int empresaId, string nome, CategoriaInsumo categoria, UnidadeMedida unidadeBase, string? marca = null, string? observacao = null) =>
        new(empresaId, nome, categoria, unidadeBase, marca, observacao);

    public void AtualizarDados(string nome, CategoriaInsumo categoria, UnidadeMedida unidadeBase, string? marca = null, string? observacao = null)
    {
        var novoNome = NormalizarNome(nome);
        var novaMarca = NormalizarMarca(marca);
        var novaObservacao = NormalizarObservacao(observacao);

        ValidarNome(novoNome);
        ValidarMarca(novaMarca);
        ValidarObservacao(novaObservacao);
        ValidarCategoria(categoria);
        ValidarUnidadeBase(unidadeBase);

        Nome = novoNome;
        NomeNormalizado = novoNome.ToUpperInvariant();
        Marca = novaMarca;
        MarcaNormalizada = novaMarca?.ToUpperInvariant() ?? string.Empty;
        Observacao = novaObservacao;
        Categoria = categoria;
        UnidadeBase = unidadeBase;
    }

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

    private static string? NormalizarMarca(string? marca)
    {
        var marcaNormalizada = Regex.Replace(marca?.Trim() ?? string.Empty, @"\s+", " ");
        return string.IsNullOrEmpty(marcaNormalizada) ? null : marcaNormalizada;
    }

    private static string? NormalizarObservacao(string? observacao)
    {
        var observacaoNormalizada = observacao?.Trim();
        return string.IsNullOrEmpty(observacaoNormalizada) ? null : observacaoNormalizada;
    }

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

    private static void ValidarMarca(string? marca)
    {
        if (marca?.Length > TamanhoMaximoMarca)
        {
            throw new ArgumentException("A marca deve possuir no máximo 80 caracteres.", "marca");
        }
    }

    private static void ValidarObservacao(string? observacao)
    {
        if (observacao?.Length > TamanhoMaximoObservacao)
        {
            throw new ArgumentException("A observação deve possuir no máximo 1000 caracteres.", "observacao");
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
