using Precificador.Core.Empresas;

namespace Precificador.Tests.Unit.Empresas;

public sealed class EmpresaTests
{
    [Fact]
    public void Criar_normaliza_nome_e_ativa_empresa()
    {
        var empresa = Empresa.Criar("  Doce   Sabor  ");
        Assert.Equal("Doce Sabor", empresa.Nome);
        Assert.Equal("DOCE SABOR", empresa.NomeNormalizado);
        Assert.Equal(Empresa.TimeZoneIdPadrao, empresa.TimeZoneId);
        Assert.True(empresa.Ativo);
    }

    [Fact]
    public void Criar_preserva_timezone_valido_normalizado()
    {
        var empresa = Empresa.Criar("Doce Sabor", " UTC ");

        Assert.Equal("UTC", empresa.TimeZoneId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Fuso/Inexistente")]
    public void Criar_rejeita_timezone_vazio_ou_invalido(string timeZoneId) =>
        Assert.Throws<ArgumentException>(() => Empresa.Criar("Doce Sabor", timeZoneId));

    [Fact]
    public void Criar_rejeita_nome_maior_que_120_caracteres() =>
        Assert.Throws<ArgumentException>(() => Empresa.Criar(new string('a', 121)));

    [Fact]
    public void Criar_rejeita_timezone_maior_que_100_caracteres() =>
        Assert.Throws<ArgumentException>(() => Empresa.Criar("Doce Sabor", new string('a', 101)));
}
