using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Precificador.Infrastructure.Autenticacao;
using Precificador.Infrastructure.Persistence;
using Precificador.Web.Empresas;

namespace Precificador.Web.Pages.Conta;

public sealed class LoginModel(SignInManager<UsuarioAplicacao> signInManager, UserManager<UsuarioAplicacao> userManager, PrecificadorDbContext context, EmpresaContext empresaContext) : PageModel
{
    [BindProperty] public InputModel Input { get; set; } = new();
    [BindProperty(SupportsGet = true)] public string? ReturnUrl { get; set; }
    public IActionResult OnGet() => User.Identity?.IsAuthenticated == true ? RedirectToPage("/Index") : Page();
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
        var empresas = await context.UsuariosEmpresas.Where(v => v.UsuarioId == usuario.Id && v.Ativo)
            .Join(context.Empresas.Where(e => e.Ativo), v => v.EmpresaId, e => e.Id, (v, e) => e.Id).ToListAsync();
        if (empresas.Count == 1) { empresaContext.Definir(empresas[0]); return LocalRedirect(ReturnUrl ?? "/"); }
        return RedirectToPage("/Empresas/Selecionar");
    }
    public sealed class InputModel { [Required, EmailAddress] public string Email { get; set; } = string.Empty; [Required, DataType(DataType.Password)] public string Senha { get; set; } = string.Empty; }
}
