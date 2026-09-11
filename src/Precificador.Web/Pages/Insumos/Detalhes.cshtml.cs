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

    public sealed record InsumoDetalhes(string Nome, string? Marca, CategoriaInsumo Categoria, UnidadeMedida UnidadeBase, bool Ativo, string? Observacao);
}
