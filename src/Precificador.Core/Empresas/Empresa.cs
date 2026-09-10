using System.Text.RegularExpressions;

namespace Precificador.Core.Empresas;

public sealed class Empresa
{
    private const int TamanhoMaximoNome = 120;
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
        if (Nome.Length > TamanhoMaximoNome) throw new ArgumentException("O nome da empresa deve possuir no máximo 120 caracteres.", nameof(nome));

        NomeNormalizado = Nome.ToUpperInvariant();
        Ativo = true;
    }

    public int Id { get; private set; }
    public string Nome { get; private set; }
    public string NomeNormalizado { get; private set; }
    public bool Ativo { get; private set; }

    public static Empresa Criar(string nome) => new(nome);

    public static Empresa CriarTecnica(int id, string nome)
    {
        var empresa = new Empresa(nome) { Id = id };
        return empresa;
    }

    public void Renomear(string nome)
    {
        Nome = NormalizarNome(nome);
        if (string.IsNullOrWhiteSpace(Nome))
        {
            throw new ArgumentException("O nome da empresa é obrigatório.", nameof(nome));
        }
        if (Nome.Length > TamanhoMaximoNome) throw new ArgumentException("O nome da empresa deve possuir no máximo 120 caracteres.", nameof(nome));

        NomeNormalizado = Nome.ToUpperInvariant();
    }

    private static string NormalizarNome(string? nome) => Regex.Replace(nome?.Trim() ?? string.Empty, @"\s+", " ");
}
