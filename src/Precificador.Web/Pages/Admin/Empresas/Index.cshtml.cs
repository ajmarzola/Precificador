using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Precificador.Core.Empresas;
using Precificador.Infrastructure.Autenticacao;
using Precificador.Infrastructure.Persistence;

namespace Precificador.Web.Pages.Admin.Empresas;

public sealed class IndexModel(PrecificadorDbContext db) : PageModel
{
    public sealed record Linha(Empresa Empresa, int Administradores);
    public IReadOnlyList<Linha> Empresas { get; private set; } = [];
    public string Filtro { get; private set; } = "Todas";
    public async Task<IActionResult> OnGetAsync(string? situacao)
    {
        Filtro = situacao ?? "Todas";
        var consulta = db.Empresas.AsNoTracking().Where(x => !x.EhTecnica);
        switch (Filtro)
        {
            case "Ativa": consulta = consulta.Where(x => x.Ativo && x.EncerradaEmUtc == null); break;
            case "Suspensa": consulta = consulta.Where(x => !x.Ativo && x.EncerradaEmUtc == null); break;
            case "Encerrada": consulta = consulta.Where(x => x.EncerradaEmUtc != null); break;
            case "Todas": break;
            default: return BadRequest();
        }
        Empresas = await consulta.OrderBy(x => x.NomeNormalizado).Select(x => new Linha(x,
            db.UsuariosEmpresas.Count(v => v.EmpresaId == x.Id && v.Ativo && v.Perfil == PerfilUsuarioEmpresa.Administrador))).ToListAsync();
        return Page();
    }
}
