using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Precificador.Infrastructure.Persistence;

namespace Precificador.Web.Pages.Configuracoes.Precificacao;

public sealed class EditarModel(PrecificadorDbContext context) : PageModel
{
    [BindProperty]
    public ConfiguracaoPrecificacaoInputModel Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        SanitizarReturnUrl();

        var configuracao = await context.ConfiguracoesPrecificacaoEmpresas
            .AsNoTracking()
            .SingleOrDefaultAsync();

        if (configuracao is null)
        {
            return NotFound();
        }

        Input = ConfiguracaoPrecificacaoFormulario.CriarInput(configuracao);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        SanitizarReturnUrl();

        var configuracao = await context.ConfiguracoesPrecificacaoEmpresas.SingleOrDefaultAsync();
        if (configuracao is null)
        {
            return NotFound();
        }

        if (!ConfiguracaoPrecificacaoFormulario.TentarObterValores(ModelState, Input, out var valores))
        {
            return Page();
        }

        try
        {
            configuracao.Atualizar(
                valores.PercentualMaoDeObra,
                valores.TarifaEnergiaKwh,
                valores.MargemPadrao,
                valores.IncrementoComercial,
                valores.ReservaComercialDesconto);
        }
        catch (ArgumentException exception)
        {
            ConfiguracaoPrecificacaoFormulario.AdicionarErroDominio(ModelState, exception);
            return Page();
        }

        await context.SaveChangesAsync();
        TempData["MensagemSucesso"] = "Configurações de precificação atualizadas com sucesso.";
        return RedirecionarAposSalvar();
    }

    private string? ObterReturnUrlLocal(string? returnUrl) =>
        !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? returnUrl
            : null;

    private void SanitizarReturnUrl()
    {
        ReturnUrl = ObterReturnUrlLocal(ReturnUrl);
        ModelState.Remove(nameof(ReturnUrl));
    }

    private IActionResult RedirecionarAposSalvar() =>
        ReturnUrl is null
            ? RedirectToPage("/Configuracoes/Precificacao")
            : LocalRedirect(ReturnUrl);
}
