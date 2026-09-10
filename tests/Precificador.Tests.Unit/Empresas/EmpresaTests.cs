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
        Assert.True(empresa.Ativo);
    }

    [Fact]
    public void Criar_rejeita_nome_maior_que_120_caracteres() =>
        Assert.Throws<ArgumentException>(() => Empresa.Criar(new string('a', 121)));
}
