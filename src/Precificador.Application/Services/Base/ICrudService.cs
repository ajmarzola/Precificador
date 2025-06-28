using Precificador.Application.Model.Base;
using Precificador.Domain.Entities.Base;
using Precificador.Domain.Filters;
using Precificador.Domain.Repository.Base;

namespace Precificador.Application.Services.Base
{
    public interface ICrudService<TModel, TEntity, TFilter, TRepository> where TModel : ModelBase where TEntity : CrudBase where TFilter : IFilter where TRepository : ICrudRepository<TEntity, TFilter>
    {
        Task<IEnumerable<TModel>?> GetAllAsync();
        Task<TModel?> GetByIdAsync(Guid id);
        Task<IEnumerable<TModel>?> GetByFilterAsync(TFilter filter);
        Task<bool> AddAsync(TModel model);
        Task<bool> UpdateAsync(TModel value);
        Task<bool> DeleteAsync(Guid id);
    }
}