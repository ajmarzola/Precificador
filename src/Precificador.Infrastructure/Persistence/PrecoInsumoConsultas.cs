using Microsoft.EntityFrameworkCore;
using Precificador.Core.Insumos;

namespace Precificador.Infrastructure.Persistence;

public static class PrecoInsumoConsultas
{
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
