using Microsoft.EntityFrameworkCore;
using Precificador.Core.Produtos;

namespace Precificador.Infrastructure.Persistence;

public static class RegistroPrecoProdutoConsultas
{
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
