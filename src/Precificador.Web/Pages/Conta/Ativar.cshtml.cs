using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Precificador.Infrastructure.Autenticacao;
using Precificador.Infrastructure.Persistence;
using Precificador.Web.Autenticacao;
using Precificador.Web.Empresas;

namespace Precificador.Web.Pages.Conta;

public sealed class AtivarModel(UserManager<UsuarioAplicacao> usuarios, PrecificadorDbContext db, EmpresaContext empresa)
    : PaginaPublicaConta(empresa)
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
        return usuario is not null && !await usuarios.HasPasswordAsync(usuario)
            && await usuarios.VerifyUserTokenAsync(usuario, TokensConta.Ativacao, TokensConta.PurposeAtivacao, token) ? usuario : null;
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
        return await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync();
            // Recarregar evita aceitar estado anterior ao início da unidade transacional.
            await db.Entry(usuario!).ReloadAsync();
            if (await ValidarAsync() is null) { LinkValido = false; return (IActionResult)Page(); }
            var result = await usuarios.AddPasswordAsync(usuario!, Input.Senha);
            if (result.Succeeded)
            {
                usuario!.EmailConfirmed = true;
                result = await usuarios.UpdateAsync(usuario);
            }
            if (!result.Succeeded)
            {
                foreach (var erro in result.Errors) ModelState.AddModelError("Input.Senha", erro.Description);
                return Page();
            }
            // AddPasswordAsync altera o security stamp pelo próprio Identity.
            await transaction.CommitAsync();
            TempData["MensagemConta"] = "Conta ativada. Você já pode entrar com sua senha.";
            return RedirectToPage("/Conta/Login");
        });
    }
}
