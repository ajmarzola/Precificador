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
    public void CA01_Reatribuir_insumo_para_outra_empresa_e_rejeitado_e_preserva_empresa_original()
    {
        var insumo = Insumo.Criar(1, "Farinha", CategoriaInsumo.MateriaPrima, UnidadeMedida.Grama);

        Assert.Throws<InvalidOperationException>(() => insumo.DefinirEmpresa(2));

        Assert.Equal(1, insumo.EmpresaId);
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

    [Fact]
    public void Criar_com_marca_normaliza_espacos_e_gera_representacao_para_comparacao()
    {
        var insumo = Insumo.Criar(7, "Farinha", CategoriaInsumo.MateriaPrima, UnidadeMedida.Grama, "  Renata   Super Premium ");

        Assert.Equal(7, insumo.EmpresaId);
        Assert.Equal("Renata Super Premium", insumo.Marca);
        Assert.Equal("RENATA SUPER PREMIUM", insumo.MarcaNormalizada);
    }

    [Fact]
    public void Criar_sem_marca_usa_nulo_e_representacao_vazia()
    {
        var insumo = Insumo.Criar(1, "Sal", CategoriaInsumo.MateriaPrima, UnidadeMedida.Grama, "   ");

        Assert.Null(insumo.Marca);
        Assert.Equal(string.Empty, insumo.MarcaNormalizada);
    }

    [Fact]
    public void Criar_rejeita_marca_maior_que_80_caracteres_apos_normalizacao()
    {
        Assert.Throws<ArgumentException>(() =>
            Insumo.Criar(1, "Farinha", CategoriaInsumo.MateriaPrima, UnidadeMedida.Grama, new string('a', 81)));
    }

    [Fact]
    public void Criar_preserva_conteudo_interno_da_observacao_e_remove_whitespace_externo()
    {
        var insumo = Insumo.Criar(1, "Farinha", CategoriaInsumo.MateriaPrima, UnidadeMedida.Grama, observacao: "  W 300\nProteína 13,5%  ");

        Assert.Equal("W 300\nProteína 13,5%", insumo.Observacao);
    }

    [Fact]
    public void Criar_converte_observacao_apenas_com_whitespace_para_nulo()
    {
        var insumo = Insumo.Criar(1, "Farinha", CategoriaInsumo.MateriaPrima, UnidadeMedida.Grama, observacao: " \r\n\t ");

        Assert.Null(insumo.Observacao);
    }

    [Fact]
    public void Criar_rejeita_observacao_maior_que_1000_caracteres()
    {
        Assert.Throws<ArgumentException>(() =>
            Insumo.Criar(1, "Farinha", CategoriaInsumo.MateriaPrima, UnidadeMedida.Grama, observacao: new string('a', 1001)));
    }
}
