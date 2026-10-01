using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Precificador.Infrastructure.Autenticacao;
using Precificador.Web.Autenticacao;
using Precificador.Web.Empresas;

namespace Precificador.Web.Pages.Conta;

public sealed class EsqueciSenhaModel(UserManager<UsuarioAplicacao> usuarios, ServicoConta conta, EmpresaContext empresa)
    : PaginaPublicaConta(empresa)
{
    [BindProperty] public EntradaEmail Input { get; set; } = new();
    public bool Indisponivel { get; private set; }
    public IActionResult OnGet() => DestinoAutenticado() ?? Page();
    public async Task<IActionResult> OnPostAsync()
    {
        if (DestinoAutenticado() is { } destino) return destino;
        if (!ModelState.IsValid) return Page();
        if (!conta.Disponivel)
        {
            Indisponivel = true;
            Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            return Page();
        }
        var usuario = await usuarios.FindByEmailAsync(Input.Email);
        if (usuario is not null) await conta.EnviarRecuperacaoAsync(usuario);
        return RedirectToPage("/Conta/EsqueciSenhaConfirmacao");
    }
    public sealed class EntradaEmail
    {
        [Required(ErrorMessage = "Informe o e-mail."), EmailAddress(ErrorMessage = "Informe um e-mail válido."), Display(Name = "E-mail")]
        public string Email { get; set; } = "";
    }
}
