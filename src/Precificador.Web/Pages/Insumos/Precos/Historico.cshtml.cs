using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Precificador.Core.Empresas;
using Precificador.Core.Insumos;
using Precificador.Infrastructure.Persistence;

namespace Precificador.Web.Pages.Insumos.Precos;

public sealed class HistoricoModel(PrecificadorDbContext context, IDataOperacionalEmpresa dataOperacional) : PageModel
{
    public InsumoResumo? Insumo { get; private set; }

    public PrecoResumo? PrecoVigente { get; private set; }

    public IReadOnlyList<PrecoHistoricoLinha> Historico { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var dataOperacionalEmpresa = dataOperacional.Hoje;

        Insumo = await context.Insumos.AsNoTracking()
            .Where(insumo => insumo.Id == id)
            .Select(insumo => new InsumoResumo(insumo.Nome, insumo.Marca, insumo.UnidadeBase, insumo.Ativo))
            .SingleOrDefaultAsync();

        if (Insumo is null)
        {
            return NotFound();
        }

        var precoVigente = await context.PrecosInsumos.AsNoTracking()
            .SelecionarVigenteAsync(id, dataOperacionalEmpresa);
        PrecoVigente = precoVigente is null ? null : PrecoResumo.Criar(precoVigente);

        var historico = await context.PrecosInsumos.AsNoTracking().ListarHistoricoAsync(id);
        Historico = historico
            .Select(preco => PrecoHistoricoLinha.Criar(preco, Classificar(preco, precoVigente?.Id, dataOperacionalEmpresa)))
            .ToList();

        return Page();
    }

    private static string Classificar(PrecoInsumo preco, int? precoVigenteId, DateOnly dataOperacionalEmpresa)
    {
        if (preco.Id == precoVigenteId)
        {
            return "Vigente";
        }

        return preco.DataReferencia > dataOperacionalEmpresa ? "Futuro" : "Anterior";
    }

    public sealed record InsumoResumo(string Nome, string? Marca, UnidadeMedida UnidadeBase, bool Ativo);

    public sealed record PrecoResumo(DateOnly DataReferencia, decimal QuantidadeCompra, decimal PrecoCompra, decimal CustoUnitario)
    {
        public static PrecoResumo Criar(PrecoInsumo preco) =>
            new(preco.DataReferencia, preco.QuantidadeCompra, preco.PrecoCompra, preco.CustoUnitario);
    }

    public sealed record PrecoHistoricoLinha(DateOnly DataReferencia, string Status, decimal QuantidadeCompra, decimal PrecoCompra, decimal CustoUnitario)
    {
        public static PrecoHistoricoLinha Criar(PrecoInsumo preco, string status) =>
            new(preco.DataReferencia, status, preco.QuantidadeCompra, preco.PrecoCompra, preco.CustoUnitario);
    }
}
