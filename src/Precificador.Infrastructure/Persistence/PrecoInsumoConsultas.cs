using Microsoft.EntityFrameworkCore;
using Precificador.Core.Insumos;

namespace Precificador.Infrastructure.Persistence;

public static class PrecoInsumoConsultas
{
    public static async Task<Dictionary<int, PrecoInsumo>> SelecionarVigentesAsync(
        this IQueryable<PrecoInsumo> precos,
        IReadOnlyCollection<int> insumoIds,
        DateOnly dataOperacionalEmpresa,
        CancellationToken cancellationToken = default)
    {
        if (insumoIds.Count == 0)
        {
            return [];
        }

        var vigentes = await precos
            .Where(preco => insumoIds.Contains(preco.InsumoId) && preco.DataReferencia <= dataOperacionalEmpresa)
            .GroupBy(preco => preco.InsumoId)
            .Select(grupo => grupo
                .OrderByDescending(preco => preco.DataReferencia)
                .ThenByDescending(preco => preco.Id)
                .First())
            .ToListAsync(cancellationToken);

        return vigentes.ToDictionary(preco => preco.InsumoId);
    }

    public static Task<PrecoInsumo?> SelecionarVigenteAsync(
        this IQueryable<PrecoInsumo> precos,
        int insumoId,
        DateOnly dataOperacionalEmpresa,
        CancellationToken cancellationToken = default) =>
        OrdenarPorDataEId(precos.Where(preco =>
                preco.InsumoId == insumoId &&
                preco.DataReferencia <= dataOperacionalEmpresa))
            .FirstOrDefaultAsync(cancellationToken);

    public static Task<List<PrecoInsumo>> ListarHistoricoAsync(
        this IQueryable<PrecoInsumo> precos,
        int insumoId,
        CancellationToken cancellationToken = default) =>
        OrdenarPorDataEId(precos.Where(preco => preco.InsumoId == insumoId))
            .ToListAsync(cancellationToken);

    private static IOrderedQueryable<PrecoInsumo> OrdenarPorDataEId(IQueryable<PrecoInsumo> precos) =>
        precos
            .OrderByDescending(preco => preco.DataReferencia)
            .ThenByDescending(preco => preco.Id);
}
