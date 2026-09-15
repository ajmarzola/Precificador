using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Precificador.Core.FichasTecnicas;
using Precificador.Infrastructure.Persistence;

namespace Precificador.Web.Pages.Produtos.FichaTecnica.Equipamentos;

public sealed class NovoModel(PrecificadorDbContext context) : PageModel
{
    [BindProperty] public UsoEquipamentoInputModel Input { get; set; } = new();
    public string? ProdutoNome { get; private set; }
    private int fichaId;
    private int empresaId;

    public async Task<IActionResult> OnGetAsync(int produtoId) => await CarregarAsync(produtoId) ? Page() : NotFoundOuRedirect(produtoId);
    public async Task<IActionResult> OnPostAsync()
    {
        var produtoId = IdRota();
        if (!await CarregarAsync(produtoId)) return NotFoundOuRedirect(produtoId);
        var potenciaValida = UsoEquipamentoFichaFormulario.TentarObterPotencia(ModelState, Input.PotenciaKw, out var potencia);
        if (Input.TempoUsoMinutos is null || Input.TempoUsoMinutos <= 0) ModelState.AddModelError("Input.TempoUsoMinutos", "O tempo de uso deve ser maior que zero.");
        if (!potenciaValida || !ModelState.IsValid) return Page();
        var nomeNormalizado = Normalizar(Input.NomeEquipamento);
        if (await context.UsosEquipamentosFicha.AnyAsync(uso => uso.FichaTecnicaId == fichaId && uso.NomeEquipamentoNormalizado == nomeNormalizado.ToUpperInvariant()))
        { ModelState.AddModelError("Input.NomeEquipamento", "Este equipamento já foi adicionado à ficha técnica."); return Page(); }
        var tempoUsoMinutos = Input.TempoUsoMinutos;
        if (tempoUsoMinutos is null) return Page();
        try { context.UsosEquipamentosFicha.Add(UsoEquipamentoFicha.Criar(empresaId, fichaId, Input.NomeEquipamento!, potencia, tempoUsoMinutos.Value)); }
        catch (ArgumentException exception) { ModelState.AddModelError("Input.NomeEquipamento", exception.Message); return Page(); }
        await context.SaveChangesAsync();
        TempData["MensagemSucesso"] = "Equipamento adicionado à ficha técnica com sucesso.";
        return RedirectToPage("/Produtos/FichaTecnica", new { id = produtoId });
    }
    private async Task<bool> CarregarAsync(int produtoId)
    {
        var produto = await context.Produtos.AsNoTracking().Where(p => p.Id == produtoId).Select(p => new { p.Nome, p.EmpresaId }).SingleOrDefaultAsync();
        if (produto is null) return false;
        ProdutoNome = produto.Nome;
        var ficha = await context.FichasTecnicas.AsNoTracking().Where(f => f.ProdutoId == produtoId).Select(f => new { f.Id, f.EmpresaId }).SingleOrDefaultAsync();
        if (ficha is null) { empresaId = 0; return false; }
        fichaId = ficha.Id; empresaId = ficha.EmpresaId; return true;
    }
    private IActionResult NotFoundOuRedirect(int produtoId)
    {
        if (ProdutoNome is not null) { TempData["MensagemAviso"] = "Defina a base da ficha técnica antes de adicionar equipamentos."; return RedirectToPage("/Produtos/FichaTecnica", new { id = produtoId }); }
        return NotFound();
    }
    private int IdRota() => Convert.ToInt32(RouteData.Values["produtoId"], CultureInfo.InvariantCulture);
    private static string Normalizar(string? nome) => System.Text.RegularExpressions.Regex.Replace(nome?.Trim() ?? string.Empty, @"\s+", " ");
    public sealed class UsoEquipamentoInputModel { public string? NomeEquipamento { get; set; } public string? PotenciaKw { get; set; } public int? TempoUsoMinutos { get; set; } }
}
