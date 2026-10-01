using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Precificador.Infrastructure.Autenticacao;
using Precificador.Web.Autenticacao;
using Precificador.Web.Empresas;

namespace Precificador.Web.Pages.Conta;

public sealed class RedefinirSenhaModel(UserManager<UsuarioAplicacao> usuarios, EmpresaContext empresa) : PaginaPublicaConta(empresa)
{
    [BindProperty(SupportsGet = true)] public string? UserId { get; set; }
    [BindProperty(SupportsGet = true)] public string? Code { get; set; }
    [BindProperty] public EntradaSenhaConta Input { get; set; } = new();
    public bool LinkValido { get; private set; }
    private async Task<UsuarioAplicacao?> ValidarAsync()
    {
        var token = TokensConta.Decodificar(Code);
        if (token is null || string.IsNullOrWhiteSpace(UserId)) return null;
        var usuario = await usuarios.FindByIdAsync(UserId);
        return usuario is not null && await usuarios.HasPasswordAsync(usuario)
            && await usuarios.VerifyUserTokenAsync(usuario, usuarios.Options.Tokens.PasswordResetTokenProvider, "ResetPassword", token) ? usuario : null;
    }
    public async Task<IActionResult> OnGetAsync()
    {
        if (DestinoAutenticado() is { } destino) return destino;
        LinkValido = await ValidarAsync() is not null;
        return Page();
    }
    public async Task<IActionResult> OnPostAsync()
    {
        if (DestinoAutenticado() is { } destino) return destino;
        var usuario = await ValidarAsync();
        LinkValido = usuario is not null;
        if (!LinkValido || !ModelState.IsValid) return Page();
        var result = await usuarios.ResetPasswordAsync(usuario!, TokensConta.Decodificar(Code)!, Input.Senha);
        if (!result.Succeeded)
        {
            foreach (var erro in result.Errors) ModelState.AddModelError("Input.Senha", erro.Description);
            return Page();
        }
        TempData["MensagemConta"] = "Senha redefinida. Você já pode entrar.";
        return RedirectToPage("/Conta/Login");
    }
}
