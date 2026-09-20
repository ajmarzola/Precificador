using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Precificador.Core.FichasTecnicas;
using Precificador.Infrastructure.Persistence;

namespace Precificador.Web.Pages.Produtos.FichaTecnica.Equipamentos;

public sealed class EditarModel(PrecificadorDbContext context) : PageModel
{
    private UsoEquipamentoFicha? usoRastreado;
    public string? ProdutoNome { get; private set; }
    [BindProperty] public NovoModel.UsoEquipamentoInputModel Input { get; set; } = new();
    public async Task<IActionResult> OnGetAsync(int produtoId, int usoId)
    { if (!await CarregarAsync(produtoId, usoId, false)) return NotFound(); Input = new() { NomeEquipamento = usoRastreado!.NomeEquipamento, PotenciaKw = UsoEquipamentoFichaFormulario.FormatarPotencia(usoRastreado.PotenciaKw), TempoUsoMinutos = usoRastreado.TempoUsoMinutos }; return Page(); }
    public async Task<IActionResult> OnPostAsync()
    { var produtoId = Id("produtoId"); var usoId = Id("usoId"); if (!await CarregarAsync(produtoId, usoId, true)) return NotFound(); var usoAtual = usoRastreado!; var potenciaValida = UsoEquipamentoFichaFormulario.TentarObterPotencia(ModelState, Input.PotenciaKw, out var potencia); if (string.IsNullOrWhiteSpace(Input.NomeEquipamento)) ModelState.AddModelError("Input.NomeEquipamento", "O nome do equipamento elétrico é obrigatório."); if (Input.TempoUsoMinutos is null || Input.TempoUsoMinutos <= 0) ModelState.AddModelError("Input.TempoUsoMinutos", "O tempo de uso deve ser maior que zero."); if (!potenciaValida || !ModelState.IsValid) return Page(); var tempoUsoMinutos = Input.TempoUsoMinutos; if (tempoUsoMinutos is null) return Page(); var normalizado = Normalizar(Input.NomeEquipamento); if (await context.UsosEquipamentosFicha.AnyAsync(u => u.FichaTecnicaId == usoAtual.FichaTecnicaId && u.Id != usoAtual.Id && u.NomeEquipamentoNormalizado == normalizado.ToUpperInvariant())) { ModelState.AddModelError("Input.NomeEquipamento", "Este equipamento elétrico já foi adicionado à ficha técnica."); return Page(); } try { usoAtual.AtualizarDados(Input.NomeEquipamento!, potencia, tempoUsoMinutos.Value); } catch (ArgumentException e) { ModelState.AddModelError("Input.NomeEquipamento", e.Message); return Page(); } await context.SaveChangesAsync(); TempData["MensagemSucesso"] = "Equipamento elétrico atualizado com sucesso."; return RedirectToPage("/Produtos/FichaTecnica", new { id = produtoId }); }
    private async Task<bool> CarregarAsync(int produtoId, int usoId, bool rastrear) { var produto = await context.Produtos.AsNoTracking().Where(p => p.Id == produtoId).Select(p => new { p.Nome }).SingleOrDefaultAsync(); if (produto is null) return false; ProdutoNome = produto.Nome; var ficha = await context.FichasTecnicas.AsNoTracking().Where(f => f.ProdutoId == produtoId).Select(f => f.Id).SingleOrDefaultAsync(); if (ficha == 0) return false; var consulta = context.UsosEquipamentosFicha.Where(u => u.Id == usoId && u.FichaTecnicaId == ficha); if (!rastrear) consulta = consulta.AsNoTracking(); var uso = await consulta.SingleOrDefaultAsync(); if (uso is null) return false; usoRastreado = uso; return true; }
    private int Id(string nome) => Convert.ToInt32(RouteData.Values[nome], CultureInfo.InvariantCulture);
    private static string Normalizar(string? nome) => System.Text.RegularExpressions.Regex.Replace(nome?.Trim() ?? string.Empty, @"\s+", " ");
}
