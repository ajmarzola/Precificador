using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Precificador.Core.Acessos;
using Precificador.Infrastructure.Persistence;

namespace Precificador.Web.Pages.Admin.Solicitacoes;

public sealed class IndexModel(PrecificadorDbContext db) : PageModel
{
    public IReadOnlyList<SolicitacaoAcessoEmpresa> Solicitacoes { get; private set; } = [];
    public string Filtro { get; private set; } = "Pendente";
    public async Task<IActionResult> OnGetAsync(string? situacao)
    {
        Filtro = situacao ?? "Pendente";
        var consulta = db.SolicitacoesAcessoEmpresas.AsNoTracking();
        if (Filtro != "Todas")
        {
            if (!Enum.TryParse<SituacaoSolicitacaoAcessoEmpresa>(Filtro, out var valor) || !Enum.IsDefined(valor) || Filtro != valor.ToString()) return BadRequest();
            consulta = consulta.Where(x => x.Situacao == valor);
        }
        Solicitacoes = await (Filtro == "Pendente" ? consulta.OrderBy(x => x.DataSolicitacaoUtc).ThenBy(x => x.Id)
            : consulta.OrderByDescending(x => x.DataSolicitacaoUtc).ThenByDescending(x => x.Id)).ToListAsync();
        return Page();
    }
}
