using Precificador.Core.Insumos;
using Precificador.Web.Apresentacao;

namespace Precificador.Tests.Integration.Web;

public sealed class InsumoRotulosTests
{
    [Fact]
    public void CA01_Categorias_possuem_rotulos_esperados_e_cobrem_todo_enum()
    {
        Assert.Equal("Matéria-prima", InsumoRotulos.Categoria(CategoriaInsumo.MateriaPrima));
        Assert.Equal("Embalagem", InsumoRotulos.Categoria(CategoriaInsumo.Embalagem));
        Assert.Equal("Consumível", InsumoRotulos.Categoria(CategoriaInsumo.Consumivel));
        Assert.Equal(
            Enum.GetValues<CategoriaInsumo>().OrderBy(categoria => (int)categoria),
            InsumoRotulos.Categorias.OrderBy(categoria => (int)categoria));
    }

    [Fact]
    public void CA02_Unidades_possuem_rotulos_esperados_e_cobrem_todo_enum()
    {
        Assert.Equal("g", InsumoRotulos.Unidade(UnidadeMedida.Grama));
        Assert.Equal("ml", InsumoRotulos.Unidade(UnidadeMedida.Mililitro));
        Assert.Equal("m", InsumoRotulos.Unidade(UnidadeMedida.Metro));
        Assert.Equal("un", InsumoRotulos.Unidade(UnidadeMedida.Unidade));
        Assert.Equal(
            Enum.GetValues<UnidadeMedida>().OrderBy(unidade => (int)unidade),
            InsumoRotulos.Unidades.OrderBy(unidade => (int)unidade));
    }
}
