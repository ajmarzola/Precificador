using Precificador.Core.Produtos;

namespace Precificador.Tests.Unit.Produtos;

public sealed class ProdutoTests
{
    [Fact]
    public void CA03_Criar_produto_valido_normaliza_campos_define_margem_e_nasce_ativo()
    {
        var produto = Produto.Criar(7, "  Agenda   2027  ", 0.30m, 5);

        Assert.Equal(7, produto.EmpresaId);
        Assert.Equal("Agenda 2027", produto.Nome);
        Assert.Equal("AGENDA 2027", produto.NomeNormalizado);
        Assert.Equal(5, produto.CategoriaProdutoId);
        Assert.Equal(0.30m, produto.MargemAlvo);
        Assert.True(produto.Ativo);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CA04_CA05_Nome_invalido_e_rejeitado_e_nome_valido_e_normalizado(string? nome)
    {
        Assert.Throws<ArgumentException>(() => Produto.Criar(1, nome!, 0.30m));
    }

    [Fact]
    public void CA04_CA05_Nome_acima_do_limite_e_rejeitado()
    {
        Assert.Throws<ArgumentException>(() => Produto.Criar(1, new string('a', 121), 0.30m));
    }

    [Fact]
    public void CA04_CA05_Nome_valido_e_normalizado_preservando_acentos()
    {
        var produto = Produto.Criar(1, "  Pão   de   Açúcar  ", 0.30m);

        Assert.Equal("Pão de Açúcar", produto.Nome);
        Assert.Equal("PÃO DE AÇÚCAR", produto.NomeNormalizado);
    }

    [Fact]
    public void CA09_CA10_Categoria_e_opcional_e_permanece_nula_quando_nao_informada()
    {
        Assert.Null(Produto.Criar(1, "Agenda", 0.30m).CategoriaProdutoId);
        Assert.Null(Produto.Criar(1, "Agenda", 0.30m, null).CategoriaProdutoId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CA09_CategoriaProdutoId_menor_ou_igual_a_zero_e_rejeitado(int categoriaProdutoId)
    {
        Assert.Throws<ArgumentException>(() => Produto.Criar(1, "Agenda", 0.30m, categoriaProdutoId));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("0.999999")]
    public void CA07_Margem_alvo_aceita_limites_validos(string margem)
    {
        var produto = Produto.Criar(1, "Agenda", decimal.Parse(margem, System.Globalization.CultureInfo.InvariantCulture));

        Assert.Equal(decimal.Parse(margem, System.Globalization.CultureInfo.InvariantCulture), produto.MargemAlvo);
    }

    [Theory]
    [InlineData("-0.01")]
    [InlineData("1")]
    public void CA07_Margem_alvo_rejeita_fora_do_intervalo(string margem)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Produto.Criar(1, "Agenda", decimal.Parse(margem, System.Globalization.CultureInfo.InvariantCulture)));
    }

    [Fact]
    public void CA13_Reatribuir_produto_para_outra_empresa_e_rejeitado_e_preserva_empresa_original()
    {
        var produto = Produto.Criar(1, "Agenda", 0.30m);

        Assert.Throws<InvalidOperationException>(() => produto.DefinirEmpresa(2));

        Assert.Equal(1, produto.EmpresaId);
    }

    [Fact]
    public void CA04_Atualizar_produto_valido_normaliza_campos_e_preserva_ownership_e_status()
    {
        var produto = Produto.Criar(7, "Agenda", 0.30m, 3);

        produto.AtualizarDados("  Calendário   2027  ", 0.255m, 9);

        Assert.Equal(7, produto.EmpresaId);
        Assert.Equal("Calendário 2027", produto.Nome);
        Assert.Equal("CALENDÁRIO 2027", produto.NomeNormalizado);
        Assert.Equal(9, produto.CategoriaProdutoId);
        Assert.Equal(0.255m, produto.MargemAlvo);
        Assert.True(produto.Ativo);
    }

    [Fact]
    public void CA05_Atualizar_nome_valida_limite_e_normaliza()
    {
        var produto = Produto.Criar(1, "Agenda", 0.30m);

        Assert.Throws<ArgumentException>(() => produto.AtualizarDados("   ", 0.30m));
        Assert.Throws<ArgumentException>(() => produto.AtualizarDados(new string('a', 121), 0.30m));

        produto.AtualizarDados("  Pão   de   Açúcar  ", 0.30m);

        Assert.Equal("Pão de Açúcar", produto.Nome);
        Assert.Equal("PÃO DE AÇÚCAR", produto.NomeNormalizado);
    }

    [Fact]
    public void CA09_CA10_Atualizar_categoria_opcional_pode_remover_trocar_e_rejeita_id_invalido()
    {
        var produto = Produto.Criar(1, "Agenda", 0.30m, 4);

        produto.AtualizarDados("Agenda", 0.30m);
        Assert.Null(produto.CategoriaProdutoId);

        produto.AtualizarDados("Agenda", 0.30m, 8);
        Assert.Equal(8, produto.CategoriaProdutoId);

        Assert.Throws<ArgumentException>(() => produto.AtualizarDados("Agenda", 0.30m, 0));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("0.999999")]
    public void CA07_Atualizar_margem_aceita_intervalo_valido(string margem)
    {
        var produto = Produto.Criar(1, "Agenda", 0.30m);
        var margemAlvo = decimal.Parse(margem, System.Globalization.CultureInfo.InvariantCulture);

        produto.AtualizarDados("Agenda", margemAlvo);

        Assert.Equal(margemAlvo, produto.MargemAlvo);
    }

    [Theory]
    [InlineData("-0.01")]
    [InlineData("1")]
    public void CA07_Atualizar_margem_rejeita_fora_do_intervalo(string margem)
    {
        var produto = Produto.Criar(1, "Agenda", 0.30m);
        var margemAlvo = decimal.Parse(margem, System.Globalization.CultureInfo.InvariantCulture);

        Assert.Throws<ArgumentOutOfRangeException>(() => produto.AtualizarDados("Agenda", margemAlvo));
    }

    [Fact]
    public void CA08_Atualizacao_invalida_nao_altera_estado_anterior()
    {
        var produto = Produto.Criar(7, "Agenda", 0.30m, 3);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            produto.AtualizarDados("Calendário 2027", 1m, 9));

        Assert.Equal(7, produto.EmpresaId);
        Assert.Equal("Agenda", produto.Nome);
        Assert.Equal("AGENDA", produto.NomeNormalizado);
        Assert.Equal(3, produto.CategoriaProdutoId);
        Assert.Equal(0.30m, produto.MargemAlvo);
        Assert.True(produto.Ativo);
    }

    [Fact]
    public void CA03_Desativar_altera_apenas_status_e_preserva_demais_dados()
    {
        var produto = Produto.Criar(7, "Agenda", 0.30m, 3);

        produto.Desativar();

        Assert.Equal(7, produto.EmpresaId);
        Assert.Equal("Agenda", produto.Nome);
        Assert.Equal("AGENDA", produto.NomeNormalizado);
        Assert.Equal(3, produto.CategoriaProdutoId);
        Assert.Equal(0.30m, produto.MargemAlvo);
        Assert.False(produto.Ativo);
    }

    [Fact]
    public void CA04_Reativar_altera_apenas_status_e_preserva_demais_dados()
    {
        var produto = Produto.Criar(7, "Agenda", 0.30m, 3);
        produto.Desativar();

        produto.Reativar();

        Assert.Equal(7, produto.EmpresaId);
        Assert.Equal("Agenda", produto.Nome);
        Assert.Equal("AGENDA", produto.NomeNormalizado);
        Assert.Equal(3, produto.CategoriaProdutoId);
        Assert.Equal(0.30m, produto.MargemAlvo);
        Assert.True(produto.Ativo);
    }

    [Fact]
    public void CA05_Desativar_e_reativar_sao_idempotentes()
    {
        var produto = Produto.Criar(1, "Agenda", 0.30m);

        produto.Desativar();
        produto.Desativar();
        Assert.False(produto.Ativo);

        produto.Reativar();
        produto.Reativar();
        Assert.True(produto.Ativo);
    }
}
