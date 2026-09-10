using Precificador.Core.Insumos;

namespace Precificador.Tests.Unit.Insumos;

public sealed class InsumoTests
{
    [Fact]
    public void Criar_com_dados_validos_define_ativo()
    {
        var insumo = Insumo.Criar(1, "Farinha", CategoriaInsumo.MateriaPrima, UnidadeMedida.Grama);

        Assert.True(insumo.Ativo);
    }

    [Fact]
    public void Criar_normaliza_espacos_do_nome()
    {
        var insumo = Insumo.Criar(1, "  Farinha   Renata  ", CategoriaInsumo.MateriaPrima, UnidadeMedida.Grama);

        Assert.Equal("Farinha Renata", insumo.Nome);
    }

    [Fact]
    public void Criar_gera_nome_normalizado_em_caixa_invariavel()
    {
        var insumo = Insumo.Criar(1, "Açúcar", CategoriaInsumo.MateriaPrima, UnidadeMedida.Grama);

        Assert.Equal("AÇÚCAR", insumo.NomeNormalizado);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Criar_rejeita_nome_vazio(string nome)
    {
        Assert.Throws<ArgumentException>(() => Insumo.Criar(1, nome, CategoriaInsumo.MateriaPrima, UnidadeMedida.Grama));
    }

    [Fact]
    public void Criar_rejeita_nome_maior_que_120_caracteres_apos_normalizacao()
    {
        var nome = new string('a', 121);

        Assert.Throws<ArgumentException>(() => Insumo.Criar(1, nome, CategoriaInsumo.MateriaPrima, UnidadeMedida.Grama));
    }

    [Fact]
    public void Criar_rejeita_categoria_invalida()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Insumo.Criar(1, "Farinha", (CategoriaInsumo)0, UnidadeMedida.Grama));
    }

    [Fact]
    public void Criar_rejeita_unidade_base_invalida()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Insumo.Criar(1, "Farinha", CategoriaInsumo.MateriaPrima, (UnidadeMedida)0));
    }

    [Fact]
    public void Categorias_preservam_valores_e_nao_possuem_ingrediente()
    {
        Assert.Equal(1, (int)CategoriaInsumo.MateriaPrima);
        Assert.Equal(2, (int)CategoriaInsumo.Embalagem);
        Assert.Equal(3, (int)CategoriaInsumo.Consumivel);
        Assert.False(Enum.GetNames<CategoriaInsumo>().Contains("Ingrediente"));
    }

    [Fact]
    public void Unidades_preservam_valores_e_incluem_metro()
    {
        Assert.Equal(1, (int)UnidadeMedida.Grama);
        Assert.Equal(2, (int)UnidadeMedida.Mililitro);
        Assert.Equal(3, (int)UnidadeMedida.Unidade);
        Assert.Equal(4, (int)UnidadeMedida.Metro);
    }

    [Fact]
    public void Criar_aceita_materia_prima_e_metro()
    {
        var insumo = Insumo.Criar(1, "Fita", CategoriaInsumo.MateriaPrima, UnidadeMedida.Metro);

        Assert.Equal(CategoriaInsumo.MateriaPrima, insumo.Categoria);
        Assert.Equal(UnidadeMedida.Metro, insumo.UnidadeBase);
    }
}
