using System.ComponentModel.DataAnnotations;
using Precificador.Core.Insumos;

namespace Precificador.Web.Pages.Insumos;

public sealed class InsumoInputModel
{
    [Display(Name = "Nome")]
    public string? Nome { get; set; }

    [Display(Name = "Marca")]
    public string? Marca { get; set; }

    public CategoriaInsumo? Categoria { get; set; }

    [Display(Name = "Unidade base")]
    public UnidadeMedida? UnidadeBase { get; set; }

    [Display(Name = "Observação")]
    public string? Observacao { get; set; }
}
