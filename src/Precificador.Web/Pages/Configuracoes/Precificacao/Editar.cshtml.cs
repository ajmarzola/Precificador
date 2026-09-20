using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Precificador.Infrastructure.Persistence;

namespace Precificador.Web.Pages.Configuracoes.Precificacao;

public sealed class EditarModel(PrecificadorDbContext context) : PageModel
{
    [BindProperty]
    public ConfiguracaoPrecificacaoInputModel Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
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
        return RedirectToPage("/Configuracoes/Precificacao");
    }
}
