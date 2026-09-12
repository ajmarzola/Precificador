using System.Text.RegularExpressions;
using Precificador.Core.Empresas;

namespace Precificador.Core.Produtos;

public sealed class Produto : IEntidadeEmpresa
{
    private const int TamanhoMaximoNome = 120;
    private const int TamanhoMaximoCategoria = 80;

    private Produto()
    {
        Nome = null!;
        NomeNormalizado = null!;
    }

    private Produto(int empresaId, string nome, decimal margemAlvo, string? categoria)
    {
        DefinirEmpresa(empresaId);
        Nome = NormalizarNome(nome);
        NomeNormalizado = Nome.ToUpperInvariant();
        Categoria = NormalizarCategoria(categoria);
        ValidarNome(Nome);
        ValidarCategoria(Categoria);
        ValidarMargemAlvo(margemAlvo);

        MargemAlvo = margemAlvo;
        Ativo = true;
    }

    public int Id { get; private set; }

    public int EmpresaId { get; private set; }

    public string Nome { get; private set; }

    public string NomeNormalizado { get; private set; }

    public string? Categoria { get; private set; }

    public decimal MargemAlvo { get; private set; }

    public bool Ativo { get; private set; }

    public static Produto Criar(int empresaId, string nome, decimal margemAlvo, string? categoria = null) =>
        new(empresaId, nome, margemAlvo, categoria);

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

    private static string? NormalizarCategoria(string? categoria)
    {
        var categoriaNormalizada = Regex.Replace(categoria?.Trim() ?? string.Empty, @"\s+", " ");
        return string.IsNullOrEmpty(categoriaNormalizada) ? null : categoriaNormalizada;
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

    private static void ValidarCategoria(string? categoria)
    {
        if (categoria?.Length > TamanhoMaximoCategoria)
        {
            throw new ArgumentException("A categoria deve possuir no máximo 80 caracteres.", "categoria");
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
