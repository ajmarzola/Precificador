using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Precificador.Infrastructure.Persistence;

namespace Precificador.Web.Pages.Insumos;

[Authorize]
public sealed class EditarModel(PrecificadorDbContext context) : PageModel
{
    [BindProperty]
    public InsumoInputModel Input { get; set; } = new();

    public bool PossuiHistorico { get; private set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var insumo = await context.Insumos.AsNoTracking()
            .Where(item => item.Id == id)
            .Select(item => new InsumoInputModel
            {
                Nome = item.Nome,
                Marca = item.Marca,
                Categoria = item.Categoria,
                UnidadeBase = item.UnidadeBase,
                Observacao = item.Observacao
            })
            .SingleOrDefaultAsync();

        if (insumo is null)
        {
            return NotFound();
        }

        Input = insumo;
        PossuiHistorico = await context.PrecosInsumos.AnyAsync(preco => preco.InsumoId == id);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var insumo = await context.Insumos.SingleOrDefaultAsync(item => item.Id == id);
        if (insumo is null)
        {
            return NotFound();
        }

        PossuiHistorico = await context.PrecosInsumos.AnyAsync(preco => preco.InsumoId == id);
        if (PossuiHistorico)
        {
            Input.Nome = insumo.Nome;
            Input.Marca = insumo.Marca;
            Input.UnidadeBase = insumo.UnidadeBase;
        }

        InsumoFormulario.ValidarCamposObrigatorios(ModelState, Input);
        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            insumo.AtualizarDados(Input.Nome!, Input.Categoria!.Value, Input.UnidadeBase!.Value, Input.Marca, Input.Observacao);
        }
        catch (ArgumentException exception)
        {
            InsumoFormulario.AdicionarErroDominio(ModelState, exception);
            return Page();
        }

        if (await context.Insumos.AnyAsync(item =>
                item.Id != id &&
                item.NomeNormalizado == insumo.NomeNormalizado &&
                item.MarcaNormalizada == insumo.MarcaNormalizada))
        {
            ModelState.AddModelError("Input.Nome", InsumoFormulario.MensagemDuplicidade);
            return Page();
        }

        await context.SaveChangesAsync();
        TempData["MensagemSucesso"] = "Insumo atualizado com sucesso.";
        return RedirectToPage("/Insumos/Detalhes", new { id });
    }
}
