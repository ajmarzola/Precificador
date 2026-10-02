using Precificador.Core.Empresas;

namespace Precificador.Tests.Unit.Empresas;

public sealed class IdentidadeVisualEmpresaTests
{
    [Theory]
    [InlineData("#aabbcc", "#AABBCC")]
    [InlineData("#0D6EFD", "#0D6EFD")]
    [InlineData("#012345", "#012345")]
    public void Cor_valida_e_normalizada(string cor, string esperada)
    {
        var identidade = IdentidadeVisualEmpresa.Criar(2, cor);
        Assert.Equal(esperada, identidade.CorPrimaria);
        Assert.Null(identidade.LogoConteudo); Assert.Null(identidade.LogoContentType);
    }

    [Theory]
    [InlineData(null)] [InlineData("")] [InlineData("red")] [InlineData("#FFF")]
    [InlineData("123456")] [InlineData("#GG1122")] [InlineData("#12345678")]
    [InlineData("#112233 ")] [InlineData("url(x)")] [InlineData("var(--x)")]
    [InlineData("#112233; color:red")] [InlineData("#１２３４５６")]
    public void Cor_invalida_nao_altera_estado(string? cor)
    {
        var identidade = IdentidadeVisualEmpresa.Criar(1, "#123456");
        Assert.Throws<ArgumentException>(() => identidade.AtualizarCor(cor!));
        Assert.Equal("#123456", identidade.CorPrimaria);
    }

    [Theory]
    [InlineData(0)] [InlineData(-1)]
    public void Empresa_deve_ser_positiva(int id) => Assert.Throws<ArgumentException>(() => IdentidadeVisualEmpresa.Criar(id, "#123456"));

    [Fact]
    public void Empresa_nao_pode_ser_reatribuida()
    {
        var identidade = IdentidadeVisualEmpresa.Criar(1, "#123456");
        identidade.DefinirEmpresa(1);
        Assert.Throws<InvalidOperationException>(() => identidade.DefinirEmpresa(2));
        Assert.Equal(1, identidade.EmpresaId);
    }

    [Theory]
    [InlineData("89504E470D0A1A0A", "image/png")]
    [InlineData("FFD8FF", "image/jpeg")]
    public void Assinatura_define_tipo_e_logo_pode_ser_removido(string hex, string tipo)
    {
        var bytes = Convert.FromHexString(hex);
        Assert.Equal(tipo, IdentidadeVisualEmpresa.DetectarContentType(bytes));
        var identidade = IdentidadeVisualEmpresa.Criar(1, "#123456");
        identidade.DefinirLogo(bytes, tipo);
        bytes[0] = 0;
        Assert.Equal(Convert.FromHexString(hex), identidade.LogoConteudo);
        identidade.RemoverLogo(); Assert.Null(identidade.LogoConteudo); Assert.Null(identidade.LogoContentType);
    }

    [Theory]
    [InlineData("")] [InlineData("89504E47")] [InlineData("FFD8")]
    [InlineData("3C7376673E")] [InlineData("474946383961")]
    public void Arquivos_vazios_truncados_e_outros_formatos_sao_rejeitados(string hex)
        => Assert.Throws<ArgumentException>(() => IdentidadeVisualEmpresa.DetectarContentType(Convert.FromHexString(hex)));

    [Fact]
    public void Limite_inclusivo_e_mutacao_invalida_preserva_logo()
    {
        var identidade = IdentidadeVisualEmpresa.Criar(1, "#123456");
        var bytes = new byte[IdentidadeVisualEmpresa.TamanhoMaximoLogo];
        Convert.FromHexString("FFD8FF").CopyTo(bytes, 0);
        identidade.DefinirLogo(bytes, "image/jpeg");
        Assert.Throws<ArgumentException>(() => identidade.DefinirLogo(bytes, "image/png"));
        Assert.Throws<ArgumentException>(() => identidade.DefinirLogo(bytes, "text/html"));
        var grande = new byte[bytes.Length + 1]; bytes.CopyTo(grande, 0);
        Assert.Throws<ArgumentException>(() => identidade.DefinirLogo(grande, "image/jpeg"));
        Assert.Equal(bytes, identidade.LogoConteudo); Assert.Equal("image/jpeg", identidade.LogoContentType);
    }
}
