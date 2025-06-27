using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Precificador.Domain.Entities.Base;
using Precificador.Domain.Filters;
using Precificador.Domain.Repository.Base;
using Precificador.Infrastructure.Data;

namespace Precificador.Infrastructure.Repository.Base
{
    public abstract class CrudRepositoryBase<TModel, TFilter>(AppDbContext context, ILogger<TModel> logger) : ICrudRepository<TModel, TFilter> where TModel : CrudBase where TFilter : IFilter
    {
        private static readonly Action<ILogger, string, Exception?> _logErrorFetchingAllRecords = LoggerMessage.Define<string>(LogLevel.Error, new EventId(1, "FetchAllError"), "Erro ao buscar todos os registros de {EntityType}");
        private static readonly Action<ILogger, Guid, string, Exception?> _logErrorFetchingById = LoggerMessage.Define<Guid, string>(LogLevel.Error, new EventId(2, "FetchByIdError"), "Erro ao buscar {EntityType} com ID {Id}");
        private static readonly Action<ILogger, string, Exception?> _logErrorAddingEntity = LoggerMessage.Define<string>(LogLevel.Error, new EventId(3, "AddEntityError"), "Erro ao inserir {EntityType}");
        private static readonly Action<ILogger, string, Exception?> _logErrorUpdatingEntity = LoggerMessage.Define<string>(LogLevel.Error, new EventId(4, "UpdateEntityError"), "Erro ao atualizar {EntityType}");
        private static readonly Action<ILogger, string, Exception?> _logErrorDeletingEntity = LoggerMessage.Define<string>(LogLevel.Error, new EventId(5, "DeleteEntityError"), "Erro ao remover {EntityType}");
        private static readonly Action<ILogger, Guid, string, Exception?> _logWarningDeletingNonExistentEntity = LoggerMessage.Define<Guid, string>(LogLevel.Warning, new EventId(6, "DeleteNonExistentEntityWarning"), "Tentativa de deletar {EntityType} com ID {Id} que não existe");
        protected static readonly Action<ILogger, string, Exception?> LogErrorFetchingByFilter = LoggerMessage.Define<string>(LogLevel.Error, new EventId(7, "FetchByFilterError"), "Erro ao buscar {EntityType} com Filtro.");

        private readonly AppDbContext _context = context;
        private readonly ILogger<TModel> _logger = logger;

        protected AppDbContext Context => _context;
        protected ILogger<TModel> Logger => _logger;

        public async Task<IEnumerable<TModel>?> GetAllAsync()
        {
            try
            {
                return await Task.FromResult(_context.Set<TModel>().AsEnumerable().Where(x => x.Ativo)).ConfigureAwait(false);
            }
            catch (InvalidOperationException ex)
            {
                _logErrorFetchingAllRecords(_logger, typeof(TModel).Name, ex);
                return [];
            }
            catch (DbUpdateException ex)
            {
                _logErrorFetchingAllRecords(_logger, typeof(TModel).Name, ex);
                return [];
            }
            catch (Exception ex)
            {
                _logErrorFetchingAllRecords(_logger, typeof(TModel).Name, ex);
                throw;
            }
        }

        public async Task<TModel?> GetByIdAsync(Guid id)
        {
            try
            {
                var result = await _context.Set<TModel>().FindAsync(id).ConfigureAwait(false);
                return result != null && result.Ativo ? result : null;
            }
            catch (InvalidOperationException ex)
            {
                _logErrorFetchingById(_logger, id, typeof(TModel).Name, ex);
                return null;
            }
            catch (DbUpdateException ex)
            {
                _logErrorFetchingById(_logger, id, typeof(TModel).Name, ex);
                return null;
            }
            catch (Exception ex)
            {
                _logErrorFetchingById(_logger, id, typeof(TModel).Name, ex);
                throw;
            }
        }

        public async Task<bool> AddAsync(TModel entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity), "The entity cannot be null.");
            }

            try
            {
                entity.SetDataCriacao();
                entity.Ativar();
                await _context.Set<TModel>().AddAsync(entity).ConfigureAwait(false);
                var saveResult = await _context.SaveChangesAsync().ConfigureAwait(false);
                return saveResult > 0;
            }
            catch (DbUpdateException ex)
            {
                _logErrorAddingEntity(_logger, typeof(TModel).Name, ex);
                return false;
            }
            catch (InvalidOperationException ex)
            {
                _logErrorAddingEntity(_logger, typeof(TModel).Name, ex);
                return false;
            }
            catch (Exception ex) when (ex is ArgumentException || ex is NotSupportedException)
            {
                _logErrorAddingEntity(_logger, typeof(TModel).Name, ex);
                return false;
            }
        }

        public async Task<bool> UpdateAsync(TModel entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity), "The entity cannot be null.");
            }

            try
            {
                entity.SetDataAlterado();
                _context.Set<TModel>().Update(entity);
                var saveResult = await _context.SaveChangesAsync().ConfigureAwait(false);
                return saveResult > 0;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logErrorUpdatingEntity(_logger, typeof(TModel).Name, ex);
                return false;
            }
            catch (DbUpdateException ex)
            {
                _logErrorUpdatingEntity(_logger, typeof(TModel).Name, ex);
                return false;
            }
            catch (InvalidOperationException ex)
            {
                _logErrorUpdatingEntity(_logger, typeof(TModel).Name, ex);
                return false;
            }
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var entity = await GetByIdAsync(id).ConfigureAwait(false);

            if (entity == null)
            {
                _logWarningDeletingNonExistentEntity(_logger, id, typeof(TModel).Name, null);
                return false;
            }

            entity.Inativar();

            try
            {
                return await UpdateAsync(entity).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logErrorDeletingEntity(_logger, typeof(TModel).Name, ex);
                return false;
            }
            catch (DbUpdateException ex)
            {
                _logErrorDeletingEntity(_logger, typeof(TModel).Name, ex);
                return false;
            }
            catch (Exception ex)
            {
                _logErrorDeletingEntity(_logger, typeof(TModel).Name, ex);
                throw;
            }
        }

        public abstract Task<IEnumerable<TModel>?> GetByFilterAsync(TFilter filter);
    }
}