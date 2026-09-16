using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Precificador.Core.Insumos;
using Precificador.Infrastructure.Persistence;
using Precificador.Web.Apresentacao;

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

        var quantidadeValida = DecimalInputParser.TentarParse(Input.QuantidadeCompra, out var quantidadeCompra);
        if (!quantidadeValida)
        {
            ModelState.AddModelError("Input.QuantidadeCompra", "A quantidade deve ser um número válido.");
        }

        var precoValido = DecimalInputParser.TentarParse(Input.PrecoCompra, out var precoCompra);
        if (!precoValido)
        {
            ModelState.AddModelError("Input.PrecoCompra", "O preço deve ser um número válido.");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            context.PrecosInsumos.Add(PrecoInsumo.Criar(insumo.EmpresaId, insumo.Id, quantidadeCompra, precoCompra, Input.DataReferencia!.Value));
            insumo.ConsolidarIdentidade();
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
        public string? QuantidadeCompra { get; set; }

        [Display(Name = "Preço total da compra")]
        public string? PrecoCompra { get; set; }

        [Display(Name = "Data de referência")]
        [DataType(DataType.Date)]
        public DateOnly? DataReferencia { get; set; }
    }

    public sealed record InsumoResumo(string Nome, string? Marca, UnidadeMedida UnidadeBase, bool Ativo);
}
