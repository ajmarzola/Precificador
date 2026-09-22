using Precificador.Core.Produtos;

namespace Precificador.Tests.Unit.Produtos;

public sealed class CategoriaProdutoTests
{
    [Fact]
    public void U1_Criar_categoria_valida_normaliza_nome_e_nasce_ativa()
    {
        var categoria = CategoriaProduto.Criar(1, "  Papelaria   escolar  ");

        Assert.Equal(1, categoria.EmpresaId);
        Assert.Equal("Papelaria escolar", categoria.Nome);
        Assert.Equal("PAPELARIA ESCOLAR", categoria.NomeNormalizado);
        Assert.True(categoria.Ativo);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void U2_Nome_vazio_ou_whitespace_e_rejeitado(string? nome)
    {
        Assert.Throws<ArgumentException>(() => CategoriaProduto.Criar(1, nome!));
    }

    [Fact]
    public void U3_Nome_acima_do_limite_e_rejeitado()
    {
        Assert.Throws<ArgumentException>(() => CategoriaProduto.Criar(1, new string('a', 81)));
    }

    [Fact]
    public void U4_Desativar_e_reativar_sao_idempotentes()
    {
        var categoria = CategoriaProduto.Criar(1, "Papelaria");

        categoria.Desativar();
        categoria.Desativar();
        Assert.False(categoria.Ativo);

        categoria.Reativar();
        categoria.Reativar();
        Assert.True(categoria.Ativo);
    }

    [Fact]
    public void U5_Renomear_normaliza_e_preserva_id_empresa_e_status()
    {
        var categoria = CategoriaProduto.Criar(7, "Papelaria");
        categoria.Desativar();

        categoria.Renomear("  Linha   premium  ");

        Assert.Equal(7, categoria.EmpresaId);
        Assert.Equal("Linha premium", categoria.Nome);
        Assert.Equal("LINHA PREMIUM", categoria.NomeNormalizado);
        Assert.False(categoria.Ativo);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void U6_Renomeacao_invalida_e_atomica_e_preserva_estado_anterior(string? novoNome)
    {
        var categoria = CategoriaProduto.Criar(7, "Papelaria");

        Assert.Throws<ArgumentException>(() => categoria.Renomear(novoNome!));

        Assert.Equal("Papelaria", categoria.Nome);
        Assert.Equal("PAPELARIA", categoria.NomeNormalizado);
    }

    [Fact]
    public void U6_Renomeacao_acima_do_limite_e_atomica_e_preserva_estado_anterior()
    {
        var categoria = CategoriaProduto.Criar(7, "Papelaria");

        Assert.Throws<ArgumentException>(() => categoria.Renomear(new string('a', 81)));

        Assert.Equal("Papelaria", categoria.Nome);
        Assert.Equal("PAPELARIA", categoria.NomeNormalizado);
    }

    [Fact]
    public void U7_Reatribuicao_de_empresa_e_rejeitada_e_preserva_empresa_original()
    {
        var categoria = CategoriaProduto.Criar(1, "Papelaria");

        Assert.Throws<InvalidOperationException>(() => categoria.DefinirEmpresa(2));

        Assert.Equal(1, categoria.EmpresaId);
    }
}
