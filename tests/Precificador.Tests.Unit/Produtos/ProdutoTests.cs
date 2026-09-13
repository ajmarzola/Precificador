using Precificador.Core.Produtos;

namespace Precificador.Tests.Unit.Produtos;

public sealed class ProdutoTests
{
    [Fact]
    public void CA03_Criar_produto_valido_normaliza_campos_define_margem_e_nasce_ativo()
    {
        var produto = Produto.Criar(7, "  Agenda   2027  ", 0.30m, "  Planners   personalizados  ");

        Assert.Equal(7, produto.EmpresaId);
        Assert.Equal("Agenda 2027", produto.Nome);
        Assert.Equal("AGENDA 2027", produto.NomeNormalizado);
        Assert.Equal("Planners personalizados", produto.Categoria);
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
    public void CA06_Categoria_opcional_normaliza_whitespace_e_respeita_limite()
    {
        Assert.Null(Produto.Criar(1, "Agenda", 0.30m).Categoria);
        Assert.Null(Produto.Criar(1, "Agenda", 0.30m, " \r\n\t ").Categoria);

        var produto = Produto.Criar(1, "Agenda", 0.30m, "  Linha   premium ");

        Assert.Equal("Linha premium", produto.Categoria);
        Assert.Throws<ArgumentException>(() => Produto.Criar(1, "Agenda 2", 0.30m, new string('a', 81)));
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
        var produto = Produto.Criar(7, "Agenda", 0.30m, "Planners");

        produto.AtualizarDados("  Calendário   2027  ", 0.255m, "  Datas   comemorativas ");

        Assert.Equal(7, produto.EmpresaId);
        Assert.Equal("Calendário 2027", produto.Nome);
        Assert.Equal("CALENDÁRIO 2027", produto.NomeNormalizado);
        Assert.Equal("Datas comemorativas", produto.Categoria);
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
    public void CA06_Atualizar_categoria_opcional_normaliza_e_valida_limite()
    {
        var produto = Produto.Criar(1, "Agenda", 0.30m, "Papelaria");

        produto.AtualizarDados("Agenda", 0.30m, " \r\n\t ");
        Assert.Null(produto.Categoria);

        produto.AtualizarDados("Agenda", 0.30m, "  Linha   premium ");
        Assert.Equal("Linha premium", produto.Categoria);
        Assert.Throws<ArgumentException>(() => produto.AtualizarDados("Agenda", 0.30m, new string('a', 81)));
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
        var produto = Produto.Criar(7, "Agenda", 0.30m, "Planners");

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            produto.AtualizarDados("Calendário 2027", 1m, "Datas"));

        Assert.Equal(7, produto.EmpresaId);
        Assert.Equal("Agenda", produto.Nome);
        Assert.Equal("AGENDA", produto.NomeNormalizado);
        Assert.Equal("Planners", produto.Categoria);
        Assert.Equal(0.30m, produto.MargemAlvo);
        Assert.True(produto.Ativo);
    }
}
