using System.ComponentModel.DataAnnotations;

namespace Precificador.Web.Pages.Produtos.Categorias;

public sealed class CategoriaProdutoInputModel
{
    [Display(Name = "Nome")]
    public string? Nome { get; set; }
}
