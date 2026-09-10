using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Precificador.Core.Empresas;
using Precificador.Core.Insumos;
using Precificador.Infrastructure.Persistence;

namespace Precificador.Web.Pages.Insumos;

[Authorize]
public sealed class NovoModel(PrecificadorDbContext context, IEmpresaContext empresaContext, ILogger<NovoModel> logger) : PageModel
{
    private const string MensagemDuplicidade = "Já existe um insumo cadastrado com esse nome e marca.";

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? MensagemSucesso => TempData["MensagemSucesso"] as string;

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ValidarCamposObrigatorios();
        if (!ModelState.IsValid)
        {
            return Page();
        }

        Insumo insumo;
        try
        {
            insumo = Insumo.Criar(empresaContext.EmpresaId!.Value, Input.Nome!, Input.Categoria!.Value, Input.UnidadeBase!.Value, Input.Marca, Input.Observacao);
        }
        catch (ArgumentException exception)
        {
            ModelState.AddModelError(CampoPara(exception.ParamName), exception.Message);
            return Page();
        }

        if (await context.Insumos.AnyAsync(item => item.NomeNormalizado == insumo.NomeNormalizado && item.MarcaNormalizada == insumo.MarcaNormalizada))
        {
            ModelState.AddModelError("Input.Nome", MensagemDuplicidade);
            return Page();
        }

        context.Insumos.Add(insumo);
        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateException exception)
        {
            logger.LogError(exception, "Falha inesperada ao cadastrar o insumo {NomeNormalizado} e a marca {MarcaNormalizada}.", insumo.NomeNormalizado, insumo.MarcaNormalizada);
            throw;
        }

        TempData["MensagemSucesso"] = "Insumo cadastrado com sucesso.";
        return RedirectToPage();
    }

    private void ValidarCamposObrigatorios()
    {
        if (!Input.Categoria.HasValue || !Enum.IsDefined(Input.Categoria.Value))
        {
            ModelState.AddModelError("Input.Categoria", "A categoria é obrigatória.");
        }

        if (!Input.UnidadeBase.HasValue || !Enum.IsDefined(Input.UnidadeBase.Value))
        {
            ModelState.AddModelError("Input.UnidadeBase", "A unidade base é obrigatória.");
        }
    }

    private static string CampoPara(string? nomeParametro) => nomeParametro switch
    {
        "marca" => "Input.Marca",
        "observacao" => "Input.Observacao",
        _ => "Input.Nome"
    };

    public sealed class InputModel
    {
        [Display(Name = "Nome")]
        public string? Nome { get; set; }

        [Display(Name = "Marca")]
        public string? Marca { get; set; }

        public CategoriaInsumo? Categoria { get; set; }

        [Display(Name = "Unidade base")]
        public UnidadeMedida? UnidadeBase { get; set; }

        [Display(Name = "Observação")]
        public string? Observacao { get; set; }
    }
}
