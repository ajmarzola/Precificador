using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Precificador.Infrastructure.Persistence;
using Precificador.Web.Empresas;

namespace Precificador.Web.Pages.Configuracoes;

public sealed class PrecificacaoModel(PrecificadorDbContext context, EmpresaContext empresaContext) : PageModel
{
    public ConfiguracaoPrecificacaoDetalhes? Configuracao { get; private set; }

    public string EmpresaNome => empresaContext.Nome ?? "não selecionada";

    public string? MensagemSucesso => TempData["MensagemSucesso"] as string;

    public async Task<IActionResult> OnGetAsync()
    {
        Configuracao = await context.ConfiguracoesPrecificacaoEmpresas.AsNoTracking()
            .Select(configuracao => new ConfiguracaoPrecificacaoDetalhes(
                configuracao.ValorHoraTrabalho,
                configuracao.TarifaEnergiaKwh,
                configuracao.MargemPadrao,
                configuracao.IncrementoComercial,
                configuracao.ReservaComercialDesconto))
            .SingleOrDefaultAsync();

        return Configuracao is null ? NotFound() : Page();
    }

    public sealed record ConfiguracaoPrecificacaoDetalhes(
        decimal? ValorHoraTrabalho,
        decimal? TarifaEnergiaKwh,
        decimal? MargemPadrao,
        decimal? IncrementoComercial,
        decimal ReservaComercialDesconto);
}
