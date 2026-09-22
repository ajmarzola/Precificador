using Precificador.Core.Produtos;

namespace Precificador.Tests.Unit.Produtos;

public sealed class CategoriaProdutoTests
{
    [Fact]
    public void U1_Criar_categoria_valida_normaliza_nome_e_nasce_ativa()
    {
        var categoria = Criar(1, "  Papelaria   escolar  ");

        Assert.Equal(1, categoria.EmpresaId);
        Assert.Equal("Papelaria escolar", categoria.Nome);
        Assert.Equal("PAPELARIA ESCOLAR", categoria.NomeNormalizado);
        Assert.True(categoria.Ativo);
        Assert.Equal(FormaCalculoDesgasteEquipamento.ValorFixoPorLote, categoria.FormaCalculoDesgasteEquipamento);
        Assert.Equal(0m, categoria.ValorDesgasteEquipamento);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void U2_Nome_vazio_ou_whitespace_e_rejeitado(string? nome)
    {
        Assert.Throws<ArgumentException>(() => Criar(1, nome!));
    }

    [Fact]
    public void U3_Nome_acima_do_limite_e_rejeitado()
    {
        Assert.Throws<ArgumentException>(() => Criar(1, new string('a', 81)));
    }

    [Fact]
    public void U4_Desativar_e_reativar_sao_idempotentes()
    {
        var categoria = Criar(1, "Papelaria");

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
        var categoria = Criar(7, "Papelaria");
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
        var categoria = Criar(7, "Papelaria");

        Assert.Throws<ArgumentException>(() => categoria.Renomear(novoNome!));

        Assert.Equal("Papelaria", categoria.Nome);
        Assert.Equal("PAPELARIA", categoria.NomeNormalizado);
    }

    [Fact]
    public void U6_Renomeacao_acima_do_limite_e_atomica_e_preserva_estado_anterior()
    {
        var categoria = Criar(7, "Papelaria");

        Assert.Throws<ArgumentException>(() => categoria.Renomear(new string('a', 81)));

        Assert.Equal("Papelaria", categoria.Nome);
        Assert.Equal("PAPELARIA", categoria.NomeNormalizado);
    }

    [Fact]
    public void U7_Reatribuicao_de_empresa_e_rejeitada_e_preserva_empresa_original()
    {
        var categoria = Criar(1, "Papelaria");

        Assert.Throws<InvalidOperationException>(() => categoria.DefinirEmpresa(2));

        Assert.Equal(1, categoria.EmpresaId);
    }

    [Fact]
    public void U8_U9_Criar_percentual_e_rejeitar_forma_ou_valor_invalidos()
    {
        var categoria = CategoriaProduto.Criar(1, "Papelaria", FormaCalculoDesgasteEquipamento.PercentualSobreInsumos, 1.25m);
        Assert.Equal(1.25m, categoria.ValorDesgasteEquipamento);
        Assert.Throws<ArgumentOutOfRangeException>(() => CategoriaProduto.Criar(1, "Inválida", (FormaCalculoDesgasteEquipamento)99, 0m));
        Assert.Throws<ArgumentOutOfRangeException>(() => CategoriaProduto.Criar(1, "Negativa", FormaCalculoDesgasteEquipamento.ValorFixoPorLote, -1m));
    }

    [Fact]
    public void U10_U11_Atualizacao_e_atomica_e_preserva_desgaste_na_situacao()
    {
        var categoria = Criar(1, "Papelaria", FormaCalculoDesgasteEquipamento.ValorFixoPorLote, 1m);
        categoria.Desativar();
        categoria.AtualizarDados("Brindes", FormaCalculoDesgasteEquipamento.PercentualSobreInsumos, .05m);
        Assert.Equal("Brindes", categoria.Nome);
        Assert.Equal(FormaCalculoDesgasteEquipamento.PercentualSobreInsumos, categoria.FormaCalculoDesgasteEquipamento);
        Assert.Equal(.05m, categoria.ValorDesgasteEquipamento);
        Assert.False(categoria.Ativo);
        Assert.Throws<ArgumentOutOfRangeException>(() => categoria.AtualizarDados("Outro", FormaCalculoDesgasteEquipamento.PercentualSobreInsumos, -1m));
        Assert.Equal("Brindes", categoria.Nome);
        Assert.Equal(.05m, categoria.ValorDesgasteEquipamento);
    }

    private static CategoriaProduto Criar(int empresaId, string nome, FormaCalculoDesgasteEquipamento forma = FormaCalculoDesgasteEquipamento.ValorFixoPorLote, decimal valor = 0m) =>
        CategoriaProduto.Criar(empresaId, nome, forma, valor);
}
