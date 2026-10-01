using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Precificador.Web.Empresas;
using Precificador.Web.Autorizacao;

namespace Precificador.Web.Pages;

public class IndexModel(EmpresaContext empresaContext) : PageModel
{
    public IActionResult OnGet()
    {
        if (User.Identity?.IsAuthenticated == true && User.IsInRole(NomesAutorizacao.SystemAdmin)) return RedirectToPage("/Admin/Index");
        return User.Identity?.IsAuthenticated == true && empresaContext.EmpresaId.HasValue
            ? RedirectToPage("/Dashboard/Index")
            : Page();
    }
}
