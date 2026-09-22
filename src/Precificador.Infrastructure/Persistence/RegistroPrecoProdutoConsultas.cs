using Microsoft.EntityFrameworkCore;
using Precificador.Core.Produtos;

namespace Precificador.Infrastructure.Persistence;

public static class RegistroPrecoProdutoConsultas
{
    public static async Task<Dictionary<int, RegistroPrecoProduto>> SelecionarAtuaisAsync(
        this IQueryable<RegistroPrecoProduto> registros,
        IReadOnlyCollection<int> produtoIds,
        CancellationToken cancellationToken = default)
    {
        if (produtoIds.Count == 0)
        {
            return [];
        }

        var atuais = await registros
            .Where(registro => produtoIds.Contains(registro.ProdutoId))
            .GroupBy(registro => registro.ProdutoId)
            .Select(grupo => grupo
                .OrderByDescending(registro => registro.DataReferencia)
                .ThenByDescending(registro => registro.Id)
                .First())
            .ToListAsync(cancellationToken);

        return atuais.ToDictionary(registro => registro.ProdutoId);
    }

    public static Task<RegistroPrecoProduto?> SelecionarAtualAsync(
        this IQueryable<RegistroPrecoProduto> registros,
        int produtoId,
        CancellationToken cancellationToken = default) =>
        registros
            .Where(registro => registro.ProdutoId == produtoId)
            .OrdenarPorAtual()
            .FirstOrDefaultAsync(cancellationToken);

    public static Task<List<RegistroPrecoProduto>> ListarHistoricoAsync(
        this IQueryable<RegistroPrecoProduto> registros,
        int produtoId,
        CancellationToken cancellationToken = default) =>
        registros
            .Where(registro => registro.ProdutoId == produtoId)
            .OrdenarPorAtual()
            .ToListAsync(cancellationToken);

    public static IOrderedQueryable<RegistroPrecoProduto> OrdenarPorAtual(
        this IQueryable<RegistroPrecoProduto> registros) =>
        registros
            .OrderByDescending(registro => registro.DataReferencia)
            .ThenByDescending(registro => registro.Id);
}
