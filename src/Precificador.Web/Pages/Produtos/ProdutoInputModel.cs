using System.ComponentModel.DataAnnotations;

namespace Precificador.Web.Pages.Produtos;

public sealed class ProdutoInputModel
{
    [Display(Name = "Nome")]
    public string? Nome { get; set; }

    [Display(Name = "Categoria")]
    public int? CategoriaProdutoId { get; set; }

    [Display(Name = "Margem-alvo (%)")]
    public string? MargemAlvoPercentual { get; set; }
}
