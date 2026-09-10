using System.Text.RegularExpressions;

namespace Precificador.Core.Empresas;

public sealed class Empresa
{
    private Empresa()
    {
        Nome = null!;
        NomeNormalizado = null!;
    }

    private Empresa(string nome)
    {
        Nome = NormalizarNome(nome);
        if (string.IsNullOrWhiteSpace(Nome))
        {
            throw new ArgumentException("O nome da empresa é obrigatório.", nameof(nome));
        }

        NomeNormalizado = Nome.ToUpperInvariant();
        Ativo = true;
    }

    public int Id { get; private set; }
    public string Nome { get; private set; }
    public string NomeNormalizado { get; private set; }
    public bool Ativo { get; private set; }

    public static Empresa Criar(string nome) => new(nome);

    public void Renomear(string nome)
    {
        Nome = NormalizarNome(nome);
        if (string.IsNullOrWhiteSpace(Nome))
        {
            throw new ArgumentException("O nome da empresa é obrigatório.", nameof(nome));
        }

        NomeNormalizado = Nome.ToUpperInvariant();
    }

    private static string NormalizarNome(string? nome) => Regex.Replace(nome?.Trim() ?? string.Empty, @"\s+", " ");
}
