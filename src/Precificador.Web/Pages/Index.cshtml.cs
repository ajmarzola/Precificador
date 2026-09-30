using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Precificador.Web.Empresas;

namespace Precificador.Web.Pages;

public class IndexModel(EmpresaContext empresaContext) : PageModel
{
    public IActionResult OnGet()
    {
        return User.Identity?.IsAuthenticated == true && empresaContext.EmpresaId.HasValue
            ? RedirectToPage("/Dashboard/Index")
            : Page();
    }
}
