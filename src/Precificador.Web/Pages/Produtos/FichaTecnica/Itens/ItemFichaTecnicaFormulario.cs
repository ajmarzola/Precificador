using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Precificador.Web.Pages.Produtos.FichaTecnica.Itens;

public static class ItemFichaTecnicaFormulario
{
    private static readonly CultureInfo CulturaBrasileira = CultureInfo.GetCultureInfo("pt-BR");

    public static bool TentarObterQuantidade(
        ModelStateDictionary modelState,
        NovoModel.ItemFichaTecnicaInputModel input,
        out decimal quantidade) =>
        TentarObterQuantidade(modelState, input.Quantidade, out quantidade);

    public static bool TentarObterQuantidade(
        ModelStateDictionary modelState,
        string? quantidadeInformada,
        out decimal quantidade)
    {
        quantidade = 0;
        quantidadeInformada = quantidadeInformada?.Trim();

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

    public static string FormatarQuantidade(decimal quantidade) =>
        quantidade.ToString("0.######", CulturaBrasileira);

    public static bool TentarObterPercentualPerda(
        ModelStateDictionary modelState,
        string? percentualInformado,
        out decimal percentualPerda)
    {
        percentualPerda = 0m;
        percentualInformado = percentualInformado?.Trim();
        if (string.IsNullOrEmpty(percentualInformado))
        {
            return true;
        }

        var cultura = percentualInformado.Contains(',') ? CulturaBrasileira : CultureInfo.InvariantCulture;
        if (!decimal.TryParse(percentualInformado, NumberStyles.Number, cultura, out var percentual) || percentual < 0 || percentual >= 100)
        {
            modelState.AddModelError("Input.PercentualPerda", "A perda esperada deve ser um percentual maior ou igual a zero e menor que 100.");
            return false;
        }

        percentualPerda = percentual / 100m;
        return true;
    }

    public static string FormatarPercentualPerda(decimal percentualPerda) =>
        (percentualPerda * 100m).ToString("0.######", CulturaBrasileira);
}
