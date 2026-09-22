using System.Text.RegularExpressions;
using Precificador.Core.Empresas;

namespace Precificador.Core.Produtos;

public sealed class Produto : IEntidadeEmpresa
{
    private const int TamanhoMaximoNome = 120;

    private Produto()
    {
        Nome = null!;
        NomeNormalizado = null!;
    }

    private Produto(int empresaId, string nome, decimal margemAlvo, int? categoriaProdutoId)
    {
        DefinirEmpresa(empresaId);
        Nome = NormalizarNome(nome);
        NomeNormalizado = Nome.ToUpperInvariant();
        ValidarNome(Nome);
        ValidarCategoriaProdutoId(categoriaProdutoId);
        ValidarMargemAlvo(margemAlvo);

        CategoriaProdutoId = categoriaProdutoId;
        MargemAlvo = margemAlvo;
        Ativo = true;
    }

    public int Id { get; private set; }

    public int EmpresaId { get; private set; }

    public string Nome { get; private set; }

    public string NomeNormalizado { get; private set; }

    public int? CategoriaProdutoId { get; private set; }

    public decimal MargemAlvo { get; private set; }

    public bool Ativo { get; private set; }

    public static Produto Criar(int empresaId, string nome, decimal margemAlvo, int? categoriaProdutoId = null) =>
        new(empresaId, nome, margemAlvo, categoriaProdutoId);

    public void Desativar() => Ativo = false;

    public void Reativar() => Ativo = true;

    public void AtualizarDados(string nome, decimal margemAlvo, int? categoriaProdutoId = null)
    {
        var nomeNormalizado = NormalizarNome(nome);
        var nomeNormalizadoComparacao = nomeNormalizado.ToUpperInvariant();

        ValidarNome(nomeNormalizado);
        ValidarCategoriaProdutoId(categoriaProdutoId);
        ValidarMargemAlvo(margemAlvo);

        Nome = nomeNormalizado;
        NomeNormalizado = nomeNormalizadoComparacao;
        CategoriaProdutoId = categoriaProdutoId;
        MargemAlvo = margemAlvo;
    }

    public void DefinirEmpresa(int empresaId)
    {
        if (empresaId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(empresaId));
        }

        if (EmpresaId != 0 && EmpresaId != empresaId)
        {
            throw new InvalidOperationException("O produto já pertence a outra empresa.");
        }

        EmpresaId = empresaId;
    }

    private static string NormalizarNome(string? nome) =>
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

    private static void ValidarCategoriaProdutoId(int? categoriaProdutoId)
    {
        if (categoriaProdutoId is <= 0)
        {
            throw new ArgumentException("A categoria informada é inválida.", nameof(categoriaProdutoId));
        }
    }

    private static void ValidarMargemAlvo(decimal margemAlvo)
    {
        if (margemAlvo < 0 || margemAlvo >= 1)
        {
            throw new ArgumentOutOfRangeException(nameof(margemAlvo), "A margem-alvo deve ser maior ou igual a 0% e menor que 100%.");
        }
    }
}
