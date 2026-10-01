using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Precificador.Core.Acessos;
using Precificador.Infrastructure.Persistence;
using Precificador.Web.Administracao;

namespace Precificador.Web.Pages.Admin.Solicitacoes;

public sealed class DetalhesModel(PrecificadorDbContext db, ServicoAdministracao administracao) : PageModel
{
    public SolicitacaoAcessoEmpresa Solicitacao { get; private set; } = null!;
    public AprovarInput Aprovacao { get; set; } = new();
    public RecusarInput Recusa { get; set; } = new();
    [TempData] public string? Mensagem { get; set; }
    public async Task<IActionResult> OnGetAsync(int id)
    {
        if (!await CarregarAsync(id)) return NotFound();
        Aprovacao.Nome = Solicitacao.NomeEmpresa;
        return Page();
    }
    public async Task<IActionResult> OnPostAprovarAsync(int id, [Bind(Prefix = "Aprovacao")] AprovarInput aprovacao)
    {
        Aprovacao = aprovacao;
        if (!await CarregarAsync(id)) return NotFound();
        if (!ModelState.IsValid) return Page();
        return await ExibirAsync(id, await administracao.AprovarAsync(id, Aprovacao.Nome, User.FindFirstValue(ClaimTypes.NameIdentifier)!));
    }
    public async Task<IActionResult> OnPostRecusarAsync(int id, [Bind(Prefix = "Recusa")] RecusarInput recusa)
    {
        Recusa = recusa;
        if (!await CarregarAsync(id)) return NotFound();
        if (!ModelState.IsValid) return Page();
        return await ExibirAsync(id, await administracao.RecusarAsync(id, Recusa.Motivo, User.FindFirstValue(ClaimTypes.NameIdentifier)!));
    }
    private async Task<bool> CarregarAsync(int id)
    {
        Solicitacao = (await db.SolicitacoesAcessoEmpresas.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id))!;
        return Solicitacao is not null;
    }
    private async Task<IActionResult> ExibirAsync(int id, ResultadoAdministracao resultado)
    {
        if (resultado.NaoEncontrado) return NotFound();
        if (resultado.Sucesso) { Mensagem = resultado.Mensagem; return RedirectToPage(new { id }); }
        ModelState.AddModelError("", resultado.Mensagem);
        await CarregarAsync(id);
        return Page();
    }
    public sealed class AprovarInput
    {
        [Required(ErrorMessage = "Informe o Nome final."), StringLength(120)] public string Nome { get; set; } = "";
    }
    public sealed class RecusarInput
    {
        [StringLength(500)] public string? Motivo { get; set; }
    }
}
