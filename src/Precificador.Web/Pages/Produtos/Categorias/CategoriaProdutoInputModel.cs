using System.ComponentModel.DataAnnotations;
using Precificador.Core.Produtos;

namespace Precificador.Web.Pages.Produtos.Categorias;

public sealed class CategoriaProdutoInputModel
{
    [Display(Name = "Nome")]
    public string? Nome { get; set; }

    [Display(Name = "Forma de cálculo do desgaste")]
    public FormaCalculoDesgasteEquipamento FormaCalculoDesgasteEquipamento { get; set; } = FormaCalculoDesgasteEquipamento.ValorFixoPorLote;

    [Display(Name = "Valor do desgaste")]
    public string? ValorDesgasteEquipamento { get; set; } = "0,00";
}
