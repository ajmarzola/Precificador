using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Precificador.Domain.Entities;
using Precificador.Domain.Filters;
using Precificador.Domain.Repository;
using Precificador.Infrastructure.Data;
using Precificador.Infrastructure.Repository.Base;

namespace Precificador.Infrastructure.Repository
{
    public class GrupoRepository(AppDbContext context, ILogger<Grupo> logger) : CrudRepositoryBase<Grupo, NomeFilter>(context, logger), IGrupoRepository
    {
        public override async Task<IEnumerable<Grupo>?> GetByFilterAsync(NomeFilter filter)
        {
            try
            {
                var query = Context.Grupos.AsQueryable().Where(c => c.Ativo);

                if ((filter != null) && filter.IsApplied())
                {
                    if (!string.IsNullOrEmpty(filter.Nome))
                    {
                        query = query.Where(c => c.Nome.Contains(filter.Nome));
                    }
                }

                return await query.ToListAsync().ConfigureAwait(false);
            }
            catch (DbUpdateException ex)
            {
                LogErrorFetchingByFilter(Logger, typeof(Grupo).Name, ex);
                return [];
            }
            catch (InvalidOperationException ex)
            {
                LogErrorFetchingByFilter(Logger, typeof(Grupo).Name, ex);
                return [];
            }
            catch (Exception ex)
            {
                LogErrorFetchingByFilter(Logger, typeof(Grupo).Name, ex);
                throw;
            }
        }
    }
}