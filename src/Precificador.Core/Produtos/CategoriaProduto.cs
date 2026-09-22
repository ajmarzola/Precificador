using System.Text.RegularExpressions;
using Precificador.Core.Empresas;

namespace Precificador.Core.Produtos;

public sealed class CategoriaProduto : IEntidadeEmpresa
{
    private const int TamanhoMaximoNome = 80;

    private CategoriaProduto()
    {
        Nome = null!;
        NomeNormalizado = null!;
    }

    private CategoriaProduto(int empresaId, string nome)
    {
        DefinirEmpresa(empresaId);
        var nomeNormalizado = NormalizarNome(nome);
        ValidarNome(nomeNormalizado);

        Nome = nomeNormalizado;
        NomeNormalizado = nomeNormalizado.ToUpperInvariant();
        Ativo = true;
    }

    public int Id { get; private set; }

    public int EmpresaId { get; private set; }

    public string Nome { get; private set; }

    public string NomeNormalizado { get; private set; }

    public bool Ativo { get; private set; }

    public static CategoriaProduto Criar(int empresaId, string nome) => new(empresaId, nome);

    public void Renomear(string nome)
    {
        var nomeNormalizado = NormalizarNome(nome);
        ValidarNome(nomeNormalizado);

        Nome = nomeNormalizado;
        NomeNormalizado = nomeNormalizado.ToUpperInvariant();
    }

    public void Desativar() => Ativo = false;

    public void Reativar() => Ativo = true;

    public void DefinirEmpresa(int empresaId)
    {
        if (empresaId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(empresaId));
        }

        if (EmpresaId != 0 && EmpresaId != empresaId)
        {
            throw new InvalidOperationException("A categoria de produto já pertence a outra empresa.");
        }

        EmpresaId = empresaId;
    }

    private static string NormalizarNome(string? nome) =>
        Regex.Replace(nome?.Trim() ?? string.Empty, @"\s+", " ");

    private static void ValidarNome(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
        {
            throw new ArgumentException("O nome da categoria é obrigatório.", nameof(nome));
        }

        if (nome.Length > TamanhoMaximoNome)
        {
            throw new ArgumentException("O nome da categoria deve possuir no máximo 80 caracteres.", nameof(nome));
        }
    }
}
