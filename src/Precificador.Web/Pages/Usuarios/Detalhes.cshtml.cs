using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Precificador.Infrastructure.Autenticacao;
using Precificador.Web.Administracao;
using Precificador.Web.Empresas;

namespace Precificador.Web.Pages.Usuarios;

public sealed class DetalhesModel(ServicoUsuariosEmpresa servico, EmpresaContext empresaContext) : PageModel
{
    public UsuarioEmpresaConsulta UsuarioEmpresa { get; private set; } = null!;
    [BindProperty] public PerfilInput Input { get; set; } = new();
    [TempData] public string? Mensagem { get; set; }
    public async Task<IActionResult> OnGetAsync(string usuarioId)
    {
        if (!await CarregarAsync(usuarioId)) return NotFound();
        Input.Perfil = UsuarioEmpresa.Perfil;
        return Page();
    }
    public async Task<IActionResult> OnPostAlterarPerfilAsync(string usuarioId)
    {
        if (!await CarregarAsync(usuarioId)) return NotFound();
        if (!ModelState.IsValid) return Page();
        var resultado = await servico.AlterarPerfilAsync(usuarioId, Input.Perfil);
        if (resultado.Sucesso && usuarioId == User.FindFirstValue(ClaimTypes.NameIdentifier)
            && Input.Perfil == PerfilUsuarioEmpresa.Operacional)
            return RedirectToPage("/Dashboard/Index");
        return await ExibirAsync(usuarioId, resultado);
    }
    public async Task<IActionResult> OnPostDesvincularAsync(string usuarioId)
    {
        var resultado = await servico.DesvincularAsync(usuarioId);
        if (resultado.Sucesso && usuarioId == User.FindFirstValue(ClaimTypes.NameIdentifier))
        {
            empresaContext.Limpar();
            return RedirectToPage("/Empresas/Selecionar");
        }
        return await ExibirAsync(usuarioId, resultado);
    }
    public async Task<IActionResult> OnPostReativarAsync(string usuarioId)
        => await ExibirAsync(usuarioId, await servico.ReativarAsync(usuarioId));
    public async Task<IActionResult> OnPostReenviarAtivacaoAsync(string usuarioId)
        => await ExibirAsync(usuarioId, await servico.ReenviarAtivacaoAsync(usuarioId));
    private async Task<bool> CarregarAsync(string usuarioId)
    {
        UsuarioEmpresa = (await servico.ConsultarAsync(usuarioId))!;
        return UsuarioEmpresa is not null;
    }
    private async Task<IActionResult> ExibirAsync(string usuarioId, ResultadoAdministracao resultado)
    {
        if (resultado.NaoEncontrado) return NotFound();
        if (resultado.Sucesso) { Mensagem = resultado.Mensagem; return RedirectToPage(new { usuarioId }); }
        ModelState.AddModelError("", resultado.Mensagem);
        if (!await CarregarAsync(usuarioId)) return NotFound();
        Input.Perfil = UsuarioEmpresa.Perfil;
        return Page();
    }
    public sealed class PerfilInput
    {
        [EnumDataType(typeof(PerfilUsuarioEmpresa))] public PerfilUsuarioEmpresa Perfil { get; set; }
    }
}
