using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Precificador.Infrastructure.Persistence;
using Precificador.Web.Empresas;

namespace Precificador.Web.Pages.Empresas;

[Authorize]
public sealed class SelecionarModel(PrecificadorDbContext context, EmpresaContext empresaContext) : PageModel
{
    [BindProperty] public int EmpresaId { get; set; }
    public List<SelectListItem> Empresas { get; private set; } = [];
    public async Task OnGetAsync() => Empresas = await ObterEmpresasAsync();
    public async Task<IActionResult> OnPostAsync()
    {
        var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var autorizada = await context.UsuariosEmpresas.AnyAsync(v => v.UsuarioId == usuarioId && v.EmpresaId == EmpresaId && v.Ativo && context.Empresas.Any(e => e.Id == EmpresaId && e.Ativo));
        if (!autorizada) { ModelState.AddModelError(string.Empty, "Empresa indisponível para este usuário."); Empresas = await ObterEmpresasAsync(); return Page(); }
        var empresa = await context.Empresas.SingleAsync(empresa => empresa.Id == EmpresaId);
        empresaContext.Definir(EmpresaId, empresa.Nome);
        return RedirectToPage("/Index");
    }
    private async Task<List<SelectListItem>> ObterEmpresasAsync()
    {
        var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        return await context.UsuariosEmpresas.Where(v => v.UsuarioId == usuarioId && v.Ativo)
            .Join(context.Empresas.Where(e => e.Ativo), v => v.EmpresaId, e => e.Id, (v, e) => new SelectListItem(e.Nome, e.Id.ToString())).ToListAsync();
    }
}
