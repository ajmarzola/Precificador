using Precificador.Core.Insumos;

namespace Precificador.Tests.Unit.Insumos;

public sealed class InsumoTests
{
    [Fact]
    public void Criar_com_dados_validos_define_ativo()
    {
        var insumo = Insumo.Criar("Farinha", CategoriaInsumo.Ingrediente, UnidadeMedida.Grama);

        Assert.True(insumo.Ativo);
    }

    [Fact]
    public void Criar_normaliza_espacos_do_nome()
    {
        var insumo = Insumo.Criar("  Farinha   Renata  ", CategoriaInsumo.Ingrediente, UnidadeMedida.Grama);

        Assert.Equal("Farinha Renata", insumo.Nome);
    }

    [Fact]
    public void Criar_gera_nome_normalizado_em_caixa_invariavel()
    {
        var insumo = Insumo.Criar("Açúcar", CategoriaInsumo.Ingrediente, UnidadeMedida.Grama);

        Assert.Equal("AÇÚCAR", insumo.NomeNormalizado);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Criar_rejeita_nome_vazio(string nome)
    {
        Assert.Throws<ArgumentException>(() => Insumo.Criar(nome, CategoriaInsumo.Ingrediente, UnidadeMedida.Grama));
    }

    [Fact]
    public void Criar_rejeita_nome_maior_que_120_caracteres_apos_normalizacao()
    {
        var nome = new string('a', 121);

        Assert.Throws<ArgumentException>(() => Insumo.Criar(nome, CategoriaInsumo.Ingrediente, UnidadeMedida.Grama));
    }

    [Fact]
    public void Criar_rejeita_categoria_invalida()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Insumo.Criar("Farinha", (CategoriaInsumo)0, UnidadeMedida.Grama));
    }

    [Fact]
    public void Criar_rejeita_unidade_base_invalida()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Insumo.Criar("Farinha", CategoriaInsumo.Ingrediente, (UnidadeMedida)0));
    }
}
