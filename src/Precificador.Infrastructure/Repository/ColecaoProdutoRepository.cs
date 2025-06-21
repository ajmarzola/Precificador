using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Precificador.Domain.Entities;
using Precificador.Domain.Filters;
using Precificador.Domain.Repository;
using Precificador.Infrastructure.Data;
using Precificador.Infrastructure.Repository.Base;
using System.Data.Common;

namespace Precificador.Infrastructure.Repository
{
    public class ColecaoProdutoRepository(AppDbContext context, ILogger<ColecaoProduto> logger) : CrudRepositoryBase<ColecaoProduto, ColecaoProdutoFilter>(context, logger), IColecaoProdutoRepository
    {
        public override async Task<IEnumerable<ColecaoProduto>?> GetByFilterAsync(ColecaoProdutoFilter filter)
        {
            try
            {
                var query = Context.ColecaoProdutos.AsQueryable().Where(c => c.Ativo);

                if ((filter != null) && filter.IsApplied())
                {
                    if (filter.ColecaoId.HasValue)
                    {
                        query = query.Where(c => c.ColecaoId == filter.ColecaoId.Value);
                    }

                    if (!string.IsNullOrEmpty(filter.ColecaoNome))
                    {
                        query = query.Where(c => c.Colecao != null && c.Colecao.Nome.Contains(filter.ColecaoNome));
                    }

                    if (filter.ProdutoId.HasValue)
                    {
                        query = query.Where(c => c.ProdutoId == filter.ProdutoId.Value);
                    }

                    if (!string.IsNullOrEmpty(filter.ProdutoNome))
                    {
                        query = query.Where(c => c.Produto != null && c.Produto.Nome.Contains(filter.ProdutoNome));
                    }
                }

                return await query.ToListAsync().ConfigureAwait(false);
            }
            catch (DbException dbEx)
            {
                LogErrorFetchingByFilter(Logger, typeof(ColecaoProduto).Name, dbEx);
                return [];
            }
            catch (InvalidOperationException invalidOpEx)
            {
                LogErrorFetchingByFilter(Logger, typeof(ColecaoProduto).Name, invalidOpEx);
                return [];
            }
            catch (Exception ex)
            {
                LogErrorFetchingByFilter(Logger, typeof(ColecaoProduto).Name, ex);
                throw;
            }
        }
    }
}