using System.ComponentModel.DataAnnotations;

namespace Precificador.Web.Pages.Produtos.Colecoes;

public sealed class ColecaoProdutoInputModel
{
    [Display(Name = "Nome")]
    public string? Nome { get; set; }

    [Display(Name = "Data de lançamento")]
    public DateOnly? DataLancamento { get; set; }

    [Display(Name = "Data de finalização")]
    public DateOnly? DataFinalizacao { get; set; }

    public List<int> CategoriaProdutoIds { get; set; } = [];
}
