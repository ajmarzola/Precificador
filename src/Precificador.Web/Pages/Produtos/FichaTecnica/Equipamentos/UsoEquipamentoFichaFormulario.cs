using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Precificador.Web.Apresentacao;

namespace Precificador.Web.Pages.Produtos.FichaTecnica.Equipamentos;

public static class UsoEquipamentoFichaFormulario
{
    private static readonly CultureInfo CulturaBrasileira = CultureInfo.GetCultureInfo("pt-BR");

    public static bool TentarObterPotencia(ModelStateDictionary modelState, string? informada, out decimal potencia)
    {
        potencia = 0;
        informada = informada?.Trim();
        if (string.IsNullOrEmpty(informada)) { modelState.AddModelError("Input.PotenciaKw", "A potência é obrigatória."); return false; }
        if (!DecimalInputParser.TentarParse(informada, out potencia)) { modelState.AddModelError("Input.PotenciaKw", "A potência deve ser um número válido."); return false; }
        if (potencia <= 0) { modelState.AddModelError("Input.PotenciaKw", "A potência deve ser maior que zero."); return false; }
        return true;
    }

    public static string FormatarPotencia(decimal potencia) => potencia.ToString("0.######", CulturaBrasileira);
    public static string FormatarConsumo(decimal consumo) => consumo.ToString("0.######", CulturaBrasileira);
}
