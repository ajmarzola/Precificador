using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Precificador.Infrastructure.Autenticacao;
using Precificador.Infrastructure.Persistence;
using Precificador.Web.Autorizacao;
using Precificador.Web.Empresas;

namespace Precificador.Web.Pages.Conta;

public sealed class LoginModel(SignInManager<UsuarioAplicacao> signInManager, UserManager<UsuarioAplicacao> userManager, PrecificadorDbContext context, EmpresaContext empresaContext) : PageModel
{
    [BindProperty] public InputModel Input { get; set; } = new();
    [BindProperty(SupportsGet = true)] public string? ReturnUrl { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            if (User.IsInRole(NomesAutorizacao.SystemAdmin)) return RedirectToPage("/Admin/Index");
            return empresaContext.EmpresaId.HasValue
                ? RedirectToPage("/Dashboard/Index")
                : RedirectToPage("/Empresas/Selecionar");
        }
        if (!await ExisteSystemAdminAsync()) return RedirectToPage("/Setup");
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        empresaContext.Limpar();
        var usuario = await userManager.FindByEmailAsync(Input.Email);
        if (usuario is null || !await userManager.CheckPasswordAsync(usuario, Input.Senha))
        {
            ModelState.AddModelError(string.Empty, "E-mail ou senha inválidos.");
            return Page();
        }
        await signInManager.SignInAsync(usuario, false);
        if (await userManager.IsInRoleAsync(usuario, NomesAutorizacao.SystemAdmin)) return RedirectToPage("/Admin/Index");
        var empresas = await context.UsuariosEmpresas.Where(v => v.UsuarioId == usuario.Id && v.Ativo)
            .Join(context.Empresas.Where(e => e.Ativo), v => v.EmpresaId, e => e.Id, (v, e) => new { e.Id, e.Nome, e.TimeZoneId }).ToListAsync();
        if (empresas.Count == 1)
        {
            empresaContext.Definir(empresas[0].Id, empresas[0].Nome, empresas[0].TimeZoneId);
            if (string.IsNullOrWhiteSpace(ReturnUrl)) return RedirectToPage("/Dashboard/Index");
            return Url.IsLocalUrl(ReturnUrl) ? LocalRedirect(ReturnUrl) : RedirectToPage("/Dashboard/Index");
        }
        return Url?.IsLocalUrl(ReturnUrl) == true
            ? RedirectToPage("/Empresas/Selecionar", new { ReturnUrl })
            : RedirectToPage("/Empresas/Selecionar");
    }
    private async Task<bool> ExisteSystemAdminAsync() => await context.UserRoles.Join(
            context.Roles, membership => membership.RoleId, role => role.Id, (membership, role) => role.Name)
        .AnyAsync(nome => nome == NomesAutorizacao.SystemAdmin);
    public sealed class InputModel { [Required, EmailAddress] public string Email { get; set; } = string.Empty; [Required, DataType(DataType.Password)] public string Senha { get; set; } = string.Empty; }
}
