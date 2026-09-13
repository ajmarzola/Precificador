using System.ComponentModel.DataAnnotations;

namespace Precificador.Web.Pages.Produtos;

public sealed class ProdutoInputModel
{
    [Display(Name = "Nome")]
    public string? Nome { get; set; }

    [Display(Name = "Categoria")]
    public string? Categoria { get; set; }

    [Display(Name = "Margem-alvo (%)")]
    public string? MargemAlvoPercentual { get; set; }
}
