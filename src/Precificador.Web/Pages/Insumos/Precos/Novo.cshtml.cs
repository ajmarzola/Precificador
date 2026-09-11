using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Precificador.Core.Insumos;
using Precificador.Infrastructure.Persistence;

namespace Precificador.Web.Pages.Insumos.Precos;

[Authorize]
public sealed class NovoModel(PrecificadorDbContext context) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public InsumoResumo? Insumo { get; private set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        if (!await CarregarInsumoAsync(id))
        {
            return NotFound();
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var insumo = await context.Insumos.SingleOrDefaultAsync(item => item.Id == id);
        if (insumo is null)
        {
            return NotFound();
        }

        Insumo = new InsumoResumo(insumo.Nome, insumo.Marca, insumo.UnidadeBase, insumo.Ativo);
        if (!Input.DataReferencia.HasValue)
        {
            ModelState.AddModelError("Input.DataReferencia", "A data de referência é obrigatória.");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            context.PrecosInsumos.Add(PrecoInsumo.Criar(insumo.EmpresaId, insumo.Id, Input.QuantidadeCompra, Input.PrecoCompra, Input.DataReferencia!.Value));
        }
        catch (ArgumentOutOfRangeException exception)
        {
            ModelState.AddModelError(exception.ParamName == "quantidadeCompra" ? "Input.QuantidadeCompra" : "Input.PrecoCompra", exception.Message);
            return Page();
        }

        await context.SaveChangesAsync();
        TempData["MensagemSucesso"] = "Preço do insumo registrado com sucesso.";
        return RedirectToPage("/Insumos/Detalhes", new { id });
    }

    private async Task<bool> CarregarInsumoAsync(int id)
    {
        Insumo = await context.Insumos.AsNoTracking().Where(item => item.Id == id)
            .Select(item => new InsumoResumo(item.Nome, item.Marca, item.UnidadeBase, item.Ativo))
            .SingleOrDefaultAsync();
        return Insumo is not null;
    }

    public sealed class InputModel
    {
        [Display(Name = "Quantidade comprada")]
        public decimal QuantidadeCompra { get; set; }

        [Display(Name = "Preço total da compra")]
        public decimal PrecoCompra { get; set; }

        [Display(Name = "Data de referência")]
        [DataType(DataType.Date)]
        public DateOnly? DataReferencia { get; set; }
    }

    public sealed record InsumoResumo(string Nome, string? Marca, UnidadeMedida UnidadeBase, bool Ativo);
}
