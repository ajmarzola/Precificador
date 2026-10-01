using Microsoft.AspNetCore.Mvc;
using Precificador.Web.Empresas;

namespace Precificador.Web.Pages.Conta;
public sealed class EsqueciSenhaConfirmacaoModel(EmpresaContext empresa) : PaginaPublicaConta(empresa)
{
    public IActionResult OnGet() => DestinoAutenticado() ?? Page();
}
