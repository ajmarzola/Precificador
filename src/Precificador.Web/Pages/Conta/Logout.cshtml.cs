using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Precificador.Infrastructure.Autenticacao;
using Precificador.Web.Empresas;

namespace Precificador.Web.Pages.Conta;

public sealed class LogoutModel(SignInManager<UsuarioAplicacao> signInManager, EmpresaContext empresaContext) : PageModel
{
    public async Task<IActionResult> OnPostAsync() { empresaContext.Limpar(); await signInManager.SignOutAsync(); return RedirectToPage("/Conta/Login"); }
}
