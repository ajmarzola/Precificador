using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Precificador.Core.Acessos;
using Precificador.Infrastructure.Persistence;

namespace Precificador.Web.Pages.Admin;

public sealed class IndexModel(PrecificadorDbContext db) : PageModel
{
    public int Pendentes { get; private set; }
    public int Ativas { get; private set; }
    public int Suspensas { get; private set; }
    public int Encerradas { get; private set; }
    public async Task OnGetAsync()
    {
        Pendentes = await db.SolicitacoesAcessoEmpresas.CountAsync(x => x.Situacao == SituacaoSolicitacaoAcessoEmpresa.Pendente);
        var reais = db.Empresas.Where(x => !x.EhTecnica);
        Ativas = await reais.CountAsync(x => x.Ativo && x.EncerradaEmUtc == null);
        Suspensas = await reais.CountAsync(x => !x.Ativo && x.EncerradaEmUtc == null);
        Encerradas = await reais.CountAsync(x => x.EncerradaEmUtc != null);
    }
}
