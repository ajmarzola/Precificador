using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Precificador.Web.Pages.Insumos;

internal static class InsumoFormulario
{
    public const string MensagemDuplicidade = "Já existe um insumo cadastrado com esse nome e marca.";
    private const string CampoCategoria = "Input.Categoria";
    private const string CampoUnidadeBase = "Input.UnidadeBase";

    public static void ValidarCamposObrigatorios(ModelStateDictionary modelState, InsumoInputModel input)
    {
        if (!input.Categoria.HasValue || !Enum.IsDefined(input.Categoria.Value))
        {
            modelState.Remove(CampoCategoria);
            modelState.AddModelError(CampoCategoria, "A categoria é obrigatória.");
        }

        if (!input.UnidadeBase.HasValue || !Enum.IsDefined(input.UnidadeBase.Value))
        {
            modelState.Remove(CampoUnidadeBase);
            modelState.AddModelError(CampoUnidadeBase, "A unidade base é obrigatória.");
        }
    }

    public static void AdicionarErroDominio(ModelStateDictionary modelState, ArgumentException exception) =>
        modelState.AddModelError(CampoPara(exception.ParamName), exception.Message);

    private static string CampoPara(string? nomeParametro) => nomeParametro switch
    {
        "marca" => "Input.Marca",
        "observacao" => "Input.Observacao",
        _ => "Input.Nome"
    };
}
