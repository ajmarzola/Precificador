using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Precificador.Web.Empresas;
using Precificador.Web.Autorizacao;
using Precificador.Core.Acessos;
using Precificador.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Precificador.Web.Pages;

public class IndexModel(EmpresaContext empresaContext, PrecificadorDbContext db, TimeProvider timeProvider) : PageModel
{
    [BindProperty]
    public SolicitacaoInput Input { get; set; } = new();

    [TempData]
    public string? MensagemSucesso { get; set; }

    public IActionResult OnGet()
    {
        if (User.Identity?.IsAuthenticated == true && User.IsInRole(NomesAutorizacao.SystemAdmin)) return RedirectToPage("/Admin/Index");
        return User.Identity?.IsAuthenticated == true && empresaContext.EmpresaId.HasValue
            ? RedirectToPage("/Dashboard/Index")
            : Page();
    }

    public async Task<IActionResult> OnPostSolicitarAcessoAsync(CancellationToken cancellationToken)
    {
        if (User.Identity?.IsAuthenticated == true)
            return User.IsInRole(NomesAutorizacao.SystemAdmin) ? RedirectToPage("/Admin/Index")
                : empresaContext.EmpresaId.HasValue ? RedirectToPage("/Dashboard/Index") : RedirectToPage("/Empresas/Selecionar");
        if (!ModelState.IsValid) return Page();
        SolicitacaoAcessoEmpresa solicitacao;
        try
        {
            solicitacao = SolicitacaoAcessoEmpresa.Criar(Input.NomeEmpresa, Input.NomeResponsavel,
                Input.EmailResponsavel, Input.Observacao, timeProvider.GetUtcNow());
        }
        catch (ArgumentException exception)
        {
            var campo = exception.ParamName switch
            {
                "nomeEmpresa" => nameof(Input.NomeEmpresa), "nomeResponsavel" => nameof(Input.NomeResponsavel),
                "emailResponsavel" => nameof(Input.EmailResponsavel), _ => nameof(Input.Observacao)
            };
            ModelState.AddModelError("Input." + campo, exception.Message.Split(" (Parameter", StringSplitOptions.None)[0]);
            return Page();
        }
        var existente = await db.SolicitacoesAcessoEmpresas.AsNoTracking().AnyAsync(x =>
            x.NomeEmpresaNormalizado == solicitacao.NomeEmpresaNormalizado &&
            x.EmailResponsavelNormalizado == solicitacao.EmailResponsavelNormalizado &&
            (x.Situacao == SituacaoSolicitacaoAcessoEmpresa.Pendente || x.Situacao == SituacaoSolicitacaoAcessoEmpresa.Aprovada), cancellationToken);
        if (!existente)
        {
            db.SolicitacoesAcessoEmpresas.Add(solicitacao);
            try { await db.SaveChangesAsync(cancellationToken); }
            catch (DbUpdateException exception) when (ViolacaoUnicidadeSolicitacaoAcesso.EhPendenciaDuplicada(exception))
            {
                db.Entry(solicitacao).State = EntityState.Detached;
            }
        }
        MensagemSucesso = "Recebemos sua solicitação de acesso. Ela será analisada e, se necessário, entraremos em contato pelo e-mail informado.";
        return RedirectToPage("/Index");
    }

    public sealed class SolicitacaoInput
    {
        [Display(Name = "Nome da Empresa")]
        public string? NomeEmpresa { get; set; }
        [Display(Name = "Nome do responsável")]
        public string? NomeResponsavel { get; set; }
        [Display(Name = "E-mail")]
        public string? EmailResponsavel { get; set; }
        [Display(Name = "Observação")]
        public string? Observacao { get; set; }
    }
}
