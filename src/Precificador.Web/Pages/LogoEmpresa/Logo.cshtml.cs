using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Precificador.Infrastructure.Persistence;
using Precificador.Web.Autorizacao;

namespace Precificador.Web.Pages.LogoEmpresa;

[Authorize(Policy = NomesAutorizacao.EmpresaAtiva)]
public sealed class LogoModel(PrecificadorDbContext db) : PageModel
{
    public async Task<IActionResult> OnGetAsync()
    {
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        Response.Headers.CacheControl = "no-store";
        var logo = await db.IdentidadesVisuaisEmpresas.AsNoTracking()
            .Select(x => new { x.LogoConteudo, x.LogoContentType }).SingleOrDefaultAsync();
        return logo?.LogoConteudo is null ? NotFound() : File(logo.LogoConteudo, logo.LogoContentType!);
    }
}
