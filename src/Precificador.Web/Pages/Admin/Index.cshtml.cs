using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Precificador.Web.Autorizacao;

namespace Precificador.Web.Pages.Admin;

[Authorize(Policy = NomesAutorizacao.SystemAdmin)]
public sealed class IndexModel : PageModel
{
    public void OnGet()
    {
    }
}
