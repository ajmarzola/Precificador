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
    [BindProperty]
    public InsumoInputModel Input { get; set; } = new();

    public string? MensagemSucesso => TempData["MensagemSucesso"] as string;

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        InsumoFormulario.ValidarCamposObrigatorios(ModelState, Input);
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
            InsumoFormulario.AdicionarErroDominio(ModelState, exception);
            return Page();
        }

        if (await context.Insumos.AnyAsync(item => item.NomeNormalizado == insumo.NomeNormalizado && item.MarcaNormalizada == insumo.MarcaNormalizada))
        {
            ModelState.AddModelError("Input.Nome", InsumoFormulario.MensagemDuplicidade);
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
}
