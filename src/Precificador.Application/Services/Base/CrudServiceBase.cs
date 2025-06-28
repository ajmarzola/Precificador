using Precificador.Application.Model.Base;
using Precificador.Domain.Entities.Base;
using Precificador.Domain.Filters;
using Precificador.Domain.Repository.Base;

namespace Precificador.Application.Services.Base
{
    public abstract class CrudServiceBase<TModel, TEntity, TFilter, TRepository>(TRepository repository) where TModel : ModelBase where TEntity : CrudBase where TFilter : IFilter where TRepository : ICrudRepository<TEntity, TFilter>
    {
        protected readonly TRepository _repository = repository;

        public virtual async Task<IEnumerable<TModel>?> GetAllAsync()
        {
            var entities = await _repository.GetAllAsync();
            return entities == null ? [] : ConvertToModel(entities);
        }

        public virtual async Task<TModel?> GetByIdAsync(Guid id)
        {
            var entity = await _repository.GetByIdAsync(id);
            return entity == null ? null : ConvertToModel(entity);
        }

        public virtual async Task<IEnumerable<TModel>?> GetByFilterAsync(TFilter filter)
        {
            var entities = await GetEntitiesByFilterAsync(filter);
            return entities == null ? [] : ConvertToModel(entities);
        }

        public virtual async Task<bool> AddAsync(TModel model)
        {
            var entity = ConvertToEntity(model);
            return await _repository.AddAsync(entity);
        }

        public virtual async Task<bool> UpdateAsync(TModel model)
        {
            var entity = await _repository.GetByIdAsync(model.Id);

            if (entity == null)
                return false;

            UpdateEntityFromModel(entity, model);
            return await _repository.UpdateAsync(entity);
        }

        public virtual async Task<bool> DeleteAsync(Guid id)
        {
            var entity = await _repository.GetByIdAsync(id);

            if (entity == null)
                return false;

            entity.Inativar();
            return await _repository.UpdateAsync(entity);
        }

        private async Task<IEnumerable<TEntity>?> GetEntitiesByFilterAsync(TFilter filter)
        {
            var entities = await _repository.GetByFilterAsync(filter);
            return entities ?? [];
        }

        public virtual IEnumerable<TModel> ConvertToModel(IEnumerable<TEntity> entity)
        {
            return entity?.Select(e => ConvertToModel(e))?.ToList() ?? Enumerable.Empty<TModel>();
        }

        protected abstract TEntity ConvertToEntity(TModel model);
        protected abstract TModel ConvertToModel(TEntity entity);
        protected abstract void UpdateEntityFromModel(TEntity entity, TModel model);
    }
}