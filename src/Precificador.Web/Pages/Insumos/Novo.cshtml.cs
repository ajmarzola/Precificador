using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Precificador.Core.Insumos;
using Precificador.Infrastructure.Persistence;

namespace Precificador.Web.Pages.Insumos;

[Authorize]
public sealed class NovoModel(PrecificadorDbContext context, ILogger<NovoModel> logger) : PageModel
{
    private const string MensagemDuplicidade = "Já existe um insumo cadastrado com esse nome.";

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
            insumo = Insumo.Criar(Input.Nome!, Input.Categoria!.Value, Input.UnidadeBase!.Value);
        }
        catch (ArgumentException exception)
        {
            ModelState.AddModelError("Input.Nome", exception.Message);
            return Page();
        }

        if (await context.Insumos.AnyAsync(item => item.NomeNormalizado == insumo.NomeNormalizado))
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
            logger.LogError(exception, "Falha inesperada ao cadastrar o insumo {NomeNormalizado}.", insumo.NomeNormalizado);
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

    public sealed class InputModel
    {
        [Display(Name = "Nome")]
        public string? Nome { get; set; }

        public CategoriaInsumo? Categoria { get; set; }

        [Display(Name = "Unidade base")]
        public UnidadeMedida? UnidadeBase { get; set; }
    }
}
