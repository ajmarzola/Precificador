using System.Text.RegularExpressions;

namespace Precificador.Core.Empresas;

public sealed class Empresa
{
    private const int TamanhoMaximoNome = 120;
    public const int TamanhoMaximoTimeZoneId = 100;
    public const string TimeZoneIdPadrao = "America/Sao_Paulo";

    private Empresa()
    {
        Nome = null!;
        NomeNormalizado = null!;
        TimeZoneId = null!;
    }

    private Empresa(string nome, string timeZoneId)
    {
        Nome = NormalizarNome(nome);
        if (string.IsNullOrWhiteSpace(Nome))
        {
            throw new ArgumentException("O nome da empresa é obrigatório.", nameof(nome));
        }
        if (Nome.Length > TamanhoMaximoNome) throw new ArgumentException("O nome da empresa deve possuir no máximo 120 caracteres.", nameof(nome));

        NomeNormalizado = Nome.ToUpperInvariant();
        TimeZoneId = ValidarTimeZone(timeZoneId);
        Ativo = true;
    }

    public int Id { get; private set; }
    public string Nome { get; private set; }
    public string NomeNormalizado { get; private set; }
    public string TimeZoneId { get; private set; }
    public bool Ativo { get; private set; }

    public static Empresa Criar(string nome) => new(nome, TimeZoneIdPadrao);

    public static Empresa Criar(string nome, string timeZoneId) => new(nome, timeZoneId);

    public static Empresa CriarTecnica(int id, string nome)
    {
        var empresa = new Empresa(nome, TimeZoneIdPadrao) { Id = id };
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

    private static string ValidarTimeZone(string? timeZoneId)
    {
        var normalizado = timeZoneId?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalizado))
        {
            throw new ArgumentException("O fuso horário da empresa é obrigatório.", nameof(timeZoneId));
        }

        if (normalizado.Length > TamanhoMaximoTimeZoneId)
        {
            throw new ArgumentException("O fuso horário da empresa deve possuir no máximo 100 caracteres.", nameof(timeZoneId));
        }

        try
        {
            TimeZoneInfo.FindSystemTimeZoneById(normalizado);
        }
        catch (TimeZoneNotFoundException exception)
        {
            throw new ArgumentException("O fuso horário da empresa é inválido.", nameof(timeZoneId), exception);
        }
        catch (InvalidTimeZoneException exception)
        {
            throw new ArgumentException("O fuso horário da empresa é inválido.", nameof(timeZoneId), exception);
        }

        return normalizado;
    }
}
