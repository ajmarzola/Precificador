using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Precificador.Core.Empresas;
using Precificador.Infrastructure.Autenticacao;
using Precificador.Infrastructure.Persistence;
using Precificador.Web.Administracao;

namespace Precificador.Web.Pages.Admin.Empresas;

public sealed class DetalhesModel(PrecificadorDbContext db, ServicoAdministracao administracao) : PageModel
{
    public Empresa Empresa { get; private set; } = null!;
    public sealed record Administrador(string Id, string? Email, bool SemSenha);
    public IReadOnlyList<Administrador> Administradores { get; private set; } = [];
    public int? SolicitacaoId { get; private set; }
    [BindProperty] public AdministradorInput Input { get; set; } = new();
    [BindProperty] public bool ConfirmarEncerramento { get; set; }
    [TempData] public string? Mensagem { get; set; }
    public async Task<IActionResult> OnGetAsync(int id) => await CarregarAsync(id) ? Page() : NotFound();
    public async Task<IActionResult> OnPostDefinirAsync(int id)
    {
        ModelState.Remove("Input.AdministradorAtual");
        if (!await CarregarAsync(id)) return NotFound();
        if (!ModelState.IsValid) return Page();
        return await ExibirAsync(id, await administracao.DefinirAdministradorAsync(id, Input.Email));
    }
    public async Task<IActionResult> OnPostSubstituirAsync(int id)
    {
        if (!await CarregarAsync(id)) return NotFound();
        if (string.IsNullOrWhiteSpace(Input.AdministradorAtual)) ModelState.AddModelError("", "Selecione o Administrador atual.");
        if (!ModelState.IsValid) return Page();
        return await ExibirAsync(id, await administracao.DefinirAdministradorAsync(id, Input.Email, Input.AdministradorAtual));
    }
    public async Task<IActionResult> OnPostReenviarAsync(int id, string usuarioId)
        => await ExibirAsync(id, await administracao.ReenviarAtivacaoAsync(id, usuarioId));
    public async Task<IActionResult> OnPostSuspenderAsync(int id)
        => await ExibirAsync(id, await administracao.AlterarSituacaoAsync(id, "Suspender"));
    public async Task<IActionResult> OnPostReativarAsync(int id)
        => await ExibirAsync(id, await administracao.AlterarSituacaoAsync(id, "Reativar"));
    public async Task<IActionResult> OnPostEncerrarAsync(int id)
        => await ExibirAsync(id, await administracao.AlterarSituacaoAsync(id, "Encerrar", ConfirmarEncerramento));
    private async Task<bool> CarregarAsync(int id)
    {
        Empresa = (await db.Empresas.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && !x.EhTecnica))!;
        if (Empresa is null) return false;
        Administradores = await (from v in db.UsuariosEmpresas
            join u in db.Users on v.UsuarioId equals u.Id
            where v.EmpresaId == id && v.Ativo && v.Perfil == PerfilUsuarioEmpresa.Administrador
            orderby u.NormalizedEmail
            select new Administrador(u.Id, u.Email, u.PasswordHash == null)).ToListAsync();
        SolicitacaoId = await db.SolicitacoesAcessoEmpresas.Where(x => x.EmpresaId == id).Select(x => (int?)x.Id).FirstOrDefaultAsync();
        return true;
    }
    private async Task<IActionResult> ExibirAsync(int id, ResultadoAdministracao resultado)
    {
        if (resultado.NaoEncontrado) return NotFound();
        if (resultado.Sucesso) { Mensagem = resultado.Mensagem; return RedirectToPage(new { id }); }
        ModelState.AddModelError("", resultado.Mensagem);
        if (!await CarregarAsync(id)) return NotFound();
        return Page();
    }
    public sealed class AdministradorInput
    {
        [Required, EmailAddress, StringLength(256)] public string Email { get; set; } = "";
        public string? AdministradorAtual { get; set; }
    }
}
