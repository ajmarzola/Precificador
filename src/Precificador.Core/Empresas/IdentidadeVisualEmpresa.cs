namespace Precificador.Core.Empresas;

public sealed class IdentidadeVisualEmpresa : IEntidadeEmpresa
{
    public const string CorPadrao = "#0D6EFD";
    public const int TamanhoMaximoLogo = 524288;
    private IdentidadeVisualEmpresa() { CorPrimaria = null!; }
    public int EmpresaId { get; private set; }
    public string CorPrimaria { get; private set; }
    public byte[]? LogoConteudo { get; private set; }
    public string? LogoContentType { get; private set; }

    public static IdentidadeVisualEmpresa Criar(int empresaId, string corPrimaria)
    {
        if (empresaId <= 0) throw new ArgumentException("Empresa inválida.", nameof(empresaId));
        return new() { EmpresaId = empresaId, CorPrimaria = NormalizarCor(corPrimaria) };
    }

    public static string NormalizarCor(string? cor)
    {
        if (cor is null || cor.Length != 7 || cor[0] != '#' ||
            cor.AsSpan(1).ContainsAnyExcept("0123456789ABCDEFabcdef"))
            throw new ArgumentException("Selecione uma cor válida no formato #RRGGBB.", nameof(cor));
        return cor.ToUpperInvariant();
    }

    public void AtualizarCor(string corPrimaria) => CorPrimaria = NormalizarCor(corPrimaria);

    public void DefinirEmpresa(int empresaId)
    {
        if (empresaId != EmpresaId) throw new InvalidOperationException("Não é permitido reatribuir a empresa da identidade visual.");
    }

    public static string DetectarContentType(ReadOnlySpan<byte> conteudo)
    {
        if (conteudo.Length > TamanhoMaximoLogo)
            throw new ArgumentException("O logo deve possuir no máximo 512 KiB.");
        if (conteudo.StartsWith(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A })) return "image/png";
        if (conteudo.StartsWith(new byte[] { 0xFF, 0xD8, 0xFF })) return "image/jpeg";
        throw new ArgumentException("O logo deve ser um arquivo PNG ou JPEG válido.");
    }

    public void DefinirLogo(byte[] conteudo, string contentType)
    {
        ArgumentNullException.ThrowIfNull(conteudo);
        if (DetectarContentType(conteudo) != contentType)
            throw new ArgumentException("O tipo do logo não corresponde ao conteúdo.", nameof(contentType));
        LogoConteudo = conteudo.ToArray();
        LogoContentType = contentType;
    }

    public void RemoverLogo() { LogoConteudo = null; LogoContentType = null; }
}
