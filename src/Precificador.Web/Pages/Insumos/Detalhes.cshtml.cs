using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Precificador.Core.Empresas;
using Precificador.Core.Insumos;
using Precificador.Infrastructure.Persistence;

namespace Precificador.Web.Pages.Insumos;

public sealed class DetalhesModel(PrecificadorDbContext context, IDataOperacionalEmpresa dataOperacional) : PageModel
{
    public InsumoDetalhes? Insumo { get; private set; }

    public PrecoVigenteResumo? PrecoVigente { get; private set; }

    public string? MensagemSucesso => TempData["MensagemSucesso"] as string;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var dataOperacionalEmpresa = dataOperacional.Hoje;

        Insumo = await context.Insumos.AsNoTracking().Where(insumo => insumo.Id == id)
            .Select(insumo => new InsumoDetalhes(insumo.Nome, insumo.Marca, insumo.Categoria, insumo.UnidadeBase, insumo.Ativo, insumo.Observacao))
            .SingleOrDefaultAsync();
        if (Insumo is null)
        {
            return NotFound();
        }

        var precoVigente = await context.PrecosInsumos.AsNoTracking()
            .SelecionarVigenteAsync(id, dataOperacionalEmpresa);
        PrecoVigente = precoVigente is null
            ? null
            : new PrecoVigenteResumo(precoVigente.DataReferencia, precoVigente.QuantidadeCompra, precoVigente.PrecoCompra, precoVigente.CustoUnitario);

        return Page();
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

    public sealed record PrecoVigenteResumo(DateOnly DataReferencia, decimal QuantidadeCompra, decimal PrecoCompra, decimal CustoUnitario);
}
