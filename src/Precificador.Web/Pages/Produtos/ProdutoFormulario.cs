using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Precificador.Web.Pages.Produtos;

public static class ProdutoFormulario
{
    private static readonly CultureInfo CulturaBrasileira = CultureInfo.GetCultureInfo("pt-BR");

    public const string MensagemDuplicidade = "Já existe um produto cadastrado com esse nome.";

    public static bool TentarObterMargemAlvo(ModelStateDictionary modelState, ProdutoInputModel input, out decimal margemAlvo)
    {
        margemAlvo = 0;
        var percentualInformado = input.MargemAlvoPercentual?.Trim();

        if (string.IsNullOrEmpty(percentualInformado))
        {
            modelState.AddModelError("Input.MargemAlvoPercentual", "A margem-alvo é obrigatória.");
            return false;
        }

        var cultura = percentualInformado.Contains(',') ? CulturaBrasileira : CultureInfo.InvariantCulture;
        if (!decimal.TryParse(percentualInformado, NumberStyles.Number, cultura, out var percentual))
        {
            modelState.AddModelError("Input.MargemAlvoPercentual", "A margem-alvo deve ser um número válido.");
            return false;
        }

        margemAlvo = percentual / 100m;
        return true;
    }

    public static string FormatarMargemAlvoPercentual(decimal margemAlvo) =>
        (margemAlvo * 100m).ToString("0.##", CulturaBrasileira);

    public static void AdicionarErroDominio(ModelStateDictionary modelState, ArgumentException exception) =>
        modelState.AddModelError(CampoPara(exception.ParamName), exception.Message);

    private static string CampoPara(string? nomeParametro) => nomeParametro switch
    {
        "categoria" => "Input.Categoria",
        "margemAlvo" => "Input.MargemAlvoPercentual",
        _ => "Input.Nome"
    };
}
