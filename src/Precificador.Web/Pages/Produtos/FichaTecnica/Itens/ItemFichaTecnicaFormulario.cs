using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Precificador.Web.Pages.Produtos.FichaTecnica.Itens;

public static class ItemFichaTecnicaFormulario
{
    private static readonly CultureInfo CulturaBrasileira = CultureInfo.GetCultureInfo("pt-BR");

    public static bool TentarObterQuantidade(
        ModelStateDictionary modelState,
        NovoModel.ItemFichaTecnicaInputModel input,
        out decimal quantidade)
    {
        quantidade = 0;
        var quantidadeInformada = input.Quantidade?.Trim();

        if (string.IsNullOrEmpty(quantidadeInformada))
        {
            modelState.AddModelError("Input.Quantidade", "A quantidade é obrigatória.");
            return false;
        }

        var cultura = quantidadeInformada.Contains(',') ? CulturaBrasileira : CultureInfo.InvariantCulture;
        if (!decimal.TryParse(quantidadeInformada, NumberStyles.Number, cultura, out quantidade))
        {
            modelState.AddModelError("Input.Quantidade", "A quantidade deve ser um número válido.");
            return false;
        }

        if (quantidade <= 0)
        {
            modelState.AddModelError("Input.Quantidade", "A quantidade deve ser maior que zero.");
            return false;
        }

        return true;
    }
}
