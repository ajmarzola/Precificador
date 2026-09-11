using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Precificador.Core.Insumos;
using Precificador.Infrastructure.Persistence;

namespace Precificador.Web.Pages.Insumos;

public sealed class DetalhesModel(PrecificadorDbContext context) : PageModel
{
    public InsumoDetalhes? Insumo { get; private set; }

    public string? MensagemSucesso => TempData["MensagemSucesso"] as string;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Insumo = await context.Insumos.AsNoTracking().Where(insumo => insumo.Id == id)
            .Select(insumo => new InsumoDetalhes(insumo.Nome, insumo.Marca, insumo.Categoria, insumo.UnidadeBase, insumo.Ativo, insumo.Observacao))
            .SingleOrDefaultAsync();
        return Insumo is null ? NotFound() : Page();
    }

    public async Task<IActionResult> OnPostDesativarAsync(int id)
    {
        var insumo = await context.Insumos.SingleOrDefaultAsync(item => item.Id == id);
        if (insumo is null)
        {
            return NotFound();
        }

        insumo.Desativar();
        await context.SaveChangesAsync();
        TempData["MensagemSucesso"] = "Insumo desativado com sucesso.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostReativarAsync(int id)
    {
        var insumo = await context.Insumos.SingleOrDefaultAsync(item => item.Id == id);
        if (insumo is null)
        {
            return NotFound();
        }

        insumo.Reativar();
        await context.SaveChangesAsync();
        TempData["MensagemSucesso"] = "Insumo reativado com sucesso.";
        return RedirectToPage(new { id });
    }

    public sealed record InsumoDetalhes(string Nome, string? Marca, CategoriaInsumo Categoria, UnidadeMedida UnidadeBase, bool Ativo, string? Observacao);
}
