using Precificador.Core.Insumos;

namespace Precificador.Web.Apresentacao;

public static class InsumoRotulos
{
    public static IReadOnlyList<CategoriaInsumo> Categorias { get; } =
    [
        CategoriaInsumo.MateriaPrima,
        CategoriaInsumo.Embalagem,
        CategoriaInsumo.Consumivel
    ];

    public static IReadOnlyList<UnidadeMedida> Unidades { get; } =
    [
        UnidadeMedida.Grama,
        UnidadeMedida.Mililitro,
        UnidadeMedida.Metro,
        UnidadeMedida.Unidade
    ];

    public static string Categoria(CategoriaInsumo categoria) => categoria switch
    {
        CategoriaInsumo.MateriaPrima => "Matéria-prima",
        CategoriaInsumo.Embalagem => "Embalagem",
        CategoriaInsumo.Consumivel => "Consumível",
        _ => categoria.ToString()
    };

    public static string Unidade(UnidadeMedida unidade) => unidade switch
    {
        UnidadeMedida.Grama => "g",
        UnidadeMedida.Mililitro => "ml",
        UnidadeMedida.Metro => "m",
        UnidadeMedida.Unidade => "un",
        _ => unidade.ToString()
    };
}
