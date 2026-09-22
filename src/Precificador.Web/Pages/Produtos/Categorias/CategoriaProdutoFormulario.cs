using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Precificador.Core.Produtos;

namespace Precificador.Web.Pages.Produtos.Categorias;

public static class CategoriaProdutoFormulario
{
    public const string MensagemDuplicidade = "Já existe uma categoria de produto cadastrada com esse nome.";

    public static bool TentarObterDesgaste(ModelStateDictionary modelState, CategoriaProdutoInputModel input, out decimal valor)
    {
        valor = 0m;
        if (!Enum.IsDefined(input.FormaCalculoDesgasteEquipamento))
        {
            modelState.AddModelError("Input.FormaCalculoDesgasteEquipamento", "A forma de cálculo do desgaste é inválida.");
            return false;
        }
        var informado = input.ValorDesgasteEquipamento?.Trim();
        var cultura = informado?.Contains(',') == true ? CultureInfo.GetCultureInfo("pt-BR") : CultureInfo.InvariantCulture;
        if (string.IsNullOrWhiteSpace(informado) || !decimal.TryParse(informado, NumberStyles.Number, cultura, out var numero))
        {
            modelState.AddModelError("Input.ValorDesgasteEquipamento", "O valor do desgaste deve ser um número válido.");
            return false;
        }
        if (input.FormaCalculoDesgasteEquipamento == FormaCalculoDesgasteEquipamento.PercentualSobreInsumos) numero /= 100m;
        if (numero < 0m)
        {
            modelState.AddModelError("Input.ValorDesgasteEquipamento", "O valor do desgaste não pode ser negativo.");
            return false;
        }
        valor = numero;
        return true;
    }

    public static string FormatarValor(FormaCalculoDesgasteEquipamento forma, decimal valor) =>
        forma == FormaCalculoDesgasteEquipamento.PercentualSobreInsumos
            ? (valor * 100m).ToString("0.######", CultureInfo.GetCultureInfo("pt-BR"))
            : valor.ToString("0.00####", CultureInfo.GetCultureInfo("pt-BR"));

    public static string Resumir(FormaCalculoDesgasteEquipamento forma, decimal valor) =>
        forma == FormaCalculoDesgasteEquipamento.PercentualSobreInsumos
            ? $"{FormatarValor(forma, valor)}% sobre os insumos"
            : $"R$ {FormatarValor(forma, valor)} por lote";
}
