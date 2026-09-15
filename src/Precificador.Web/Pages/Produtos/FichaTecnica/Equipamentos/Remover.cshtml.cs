using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Precificador.Core.FichasTecnicas;
using Precificador.Infrastructure.Persistence;

namespace Precificador.Web.Pages.Produtos.FichaTecnica.Equipamentos;
public sealed class RemoverModel(PrecificadorDbContext context) : PageModel
{
    private UsoEquipamentoFicha? uso; public string? NomeEquipamento { get; private set; }
    public async Task<IActionResult> OnGetAsync(int produtoId, int usoId) => await CarregarAsync(produtoId, usoId, false) ? Page() : NotFound();
    public async Task<IActionResult> OnPostAsync() { var produtoId = Id("produtoId"); if (!await CarregarAsync(produtoId, Id("usoId"), true)) return NotFound(); context.UsosEquipamentosFicha.Remove(uso!); await context.SaveChangesAsync(); TempData["MensagemSucesso"] = "Equipamento removido da ficha técnica com sucesso."; return RedirectToPage("/Produtos/FichaTecnica", new { id = produtoId }); }
    private async Task<bool> CarregarAsync(int produtoId, int usoId, bool rastrear) { var ficha = await (from p in context.Produtos where p.Id == produtoId join f in context.FichasTecnicas on p.Id equals f.ProdutoId select f.Id).SingleOrDefaultAsync(); if (ficha == 0) return false; var consulta = context.UsosEquipamentosFicha.Where(u => u.Id == usoId && u.FichaTecnicaId == ficha); if (!rastrear) consulta = consulta.AsNoTracking(); uso = await consulta.SingleOrDefaultAsync(); NomeEquipamento = uso?.NomeEquipamento; return uso is not null; }
    private int Id(string nome) => Convert.ToInt32(RouteData.Values[nome], CultureInfo.InvariantCulture);
}
