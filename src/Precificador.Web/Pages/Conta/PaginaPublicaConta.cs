using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Precificador.Web.Autorizacao;
using Precificador.Web.Empresas;

namespace Precificador.Web.Pages.Conta;

public abstract class PaginaPublicaConta(EmpresaContext empresa) : PageModel
{
    protected IActionResult? DestinoAutenticado() => User.Identity?.IsAuthenticated != true ? null
        : User.IsInRole(NomesAutorizacao.SystemAdmin) ? RedirectToPage("/Admin/Index")
        : empresa.EmpresaId.HasValue ? RedirectToPage("/Dashboard/Index") : RedirectToPage("/Empresas/Selecionar");
}
public sealed class EntradaSenhaConta
{
    [Required, DataType(DataType.Password), Display(Name = "Nova senha")]
    public string Senha { get; set; } = "";
    [Required, DataType(DataType.Password), Compare(nameof(Senha), ErrorMessage = "As senhas não coincidem."), Display(Name = "Confirmação")]
    public string Confirmacao { get; set; } = "";
}
