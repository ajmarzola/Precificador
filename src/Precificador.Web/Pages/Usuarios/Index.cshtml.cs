using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Precificador.Infrastructure.Autenticacao;
using Precificador.Web.Administracao;

namespace Precificador.Web.Pages.Usuarios;

public sealed class IndexModel(ServicoUsuariosEmpresa servico) : PageModel
{
    [BindProperty] public AdicionarInput Input { get; set; } = new();
    public string Situacao { get; private set; } = "Ativos";
    public IReadOnlyList<UsuarioEmpresaConsulta> Usuarios { get; private set; } = [];
    [TempData] public string? Mensagem { get; set; }

    public async Task<IActionResult> OnGetAsync(string situacao = "Ativos")
    {
        if (situacao is not ("Ativos" or "Inativos" or "Todos")) return BadRequest();
        Situacao = situacao;
        Usuarios = await servico.ListarAsync(situacao);
        return Page();
    }

    public async Task<IActionResult> OnPostAdicionarAsync()
    {
        Input.Email = Input.Email.Trim();
        if (ModelState.IsValid)
        {
            var resultado = await servico.AdicionarAsync(Input.Email, Input.Perfil);
            if (resultado.Sucesso) { Mensagem = resultado.Mensagem; return RedirectToPage(); }
            ModelState.AddModelError("", resultado.Mensagem);
        }
        Usuarios = await servico.ListarAsync(Situacao);
        return Page();
    }

    public sealed class AdicionarInput
    {
        private string email = "";
        [Required, EmailAddress, StringLength(256)] public string Email { get => email; set => email = value?.Trim() ?? ""; }
        [EnumDataType(typeof(PerfilUsuarioEmpresa))] public PerfilUsuarioEmpresa Perfil { get; set; }
    }
}
