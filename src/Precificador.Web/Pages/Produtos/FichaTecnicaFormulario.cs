using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Precificador.Web.Pages.Produtos;

public static class FichaTecnicaFormulario
{
    private static readonly CultureInfo CulturaBrasileira = CultureInfo.GetCultureInfo("pt-BR");

    public static bool TentarObterRendimento(
        ModelStateDictionary modelState,
        FichaTecnicaModel.FichaTecnicaInputModel input,
        out decimal rendimento)
    {
        rendimento = 0;
        var rendimentoInformado = input.Rendimento?.Trim();

        if (string.IsNullOrEmpty(rendimentoInformado))
        {
            modelState.AddModelError("Input.Rendimento", "O rendimento é obrigatório.");
            return false;
        }

        var cultura = rendimentoInformado.Contains(',') ? CulturaBrasileira : CultureInfo.InvariantCulture;
        if (!decimal.TryParse(rendimentoInformado, NumberStyles.Number, cultura, out rendimento))
        {
            modelState.AddModelError("Input.Rendimento", "O rendimento deve ser um número válido.");
            return false;
        }

        if (rendimento <= 0)
        {
            modelState.AddModelError("Input.Rendimento", "O rendimento deve ser maior que zero.");
            return false;
        }

        return true;
    }

    public static string FormatarRendimento(decimal rendimento) =>
        rendimento.ToString("0.######", CulturaBrasileira);
}
