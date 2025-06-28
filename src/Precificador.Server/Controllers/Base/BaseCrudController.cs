using Microsoft.AspNetCore.Mvc;
using Precificador.Application.Model.Base;
using Precificador.Application.Services.Base;
using Precificador.Domain.Entities.Base;
using Precificador.Domain.Filters;
using Precificador.Domain.Repository.Base;

namespace Precificador.Server.Controllers.Base
{
    [Route("api/[controller]")]
    [ApiController]
    public abstract class BaseCrudController<TModel, TEntity, TFilter, TService, TRepository>(TService service, ILogger logger) : ControllerBase where TModel : ModelBase where TEntity : CrudBase where TFilter : IFilter where TService : ICrudService<TModel, TEntity, TFilter, TRepository> where TRepository : ICrudRepository<TEntity, TFilter>
    {
        protected TService _service = service;
        protected ILogger _logger = logger;

        /// <summary>
        /// Retorna todos os registros cadastrados.
        /// </summary>
        /// <returns>
        /// - 200 OK com a lista de registros
        /// - 204 No Content se não houver registros
        /// - 500 Bad Request em caso de erro.
        /// </returns>
        [HttpGet]
        public virtual async Task<IActionResult> GetAll()
        {
            _logger.LogInformation("Lendo dados de {Entity} às {Time}", typeof(TEntity), DateTime.UtcNow);

            try
            {
                var result = await _service.GetAllAsync();

                return result == null || !result.Any() ? NoContent() : Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao listar registros.");
                return BadRequest("Erro ao listar registros.");
            }
        }

        /// <summary>
        /// Retorna o registros pelo ID.
        /// </summary>
        /// <param name="id">Id do registro a ser consultado</param>
        /// <returns>
        /// - 200 OK com o registro encontrado
        /// - 204 No Content se não encontrar o registro
        /// - 500 Bad Request em caso de erro.
        /// </returns>
        [HttpGet("ById")]
        public virtual async Task<IActionResult> GetById([FromBody] Guid id)
        {
            _logger.LogInformation("Lendo dados de {Entity} às {Time} com id {id}", typeof(TEntity), DateTime.UtcNow, id);

            try
            {
                var result = await _service.GetByIdAsync(id);

                return result == null ? NoContent() : Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao consultar registro.");
                return BadRequest("Erro ao consultar registro.");
            }
        }

        /// <summary>
        /// Retorna todos os registros conforme filtro.
        /// </summary>
        /// <param name="filter">Filtro a ser aplicado na consulta</param>
        /// <returns>
        /// - 200 OK com a lista de registros
        /// - 204 No Content se não houver registros
        /// - 500 Bad Request em caso de erro.
        /// </returns>
        [HttpGet("ByFilter")]
        public virtual async Task<IActionResult> GetByFilterAsync([FromBody] TFilter filter)
        {
            _logger.LogInformation("Lendo dados de {Entity} às {Time} por filtro", typeof(TEntity), DateTime.UtcNow);

            try
            {
                if (filter == null)
                {
                    return BadRequest("Failed to deserialize filter parameter.");
                }

                var result = await _service.GetByFilterAsync(filter);

                return result == null || !result.Any() ? NoContent() : Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao listar registros.");
                return BadRequest("Erro ao listar registros.");
            }
        }

        /// <summary>
        /// Cadastra novo registro.
        /// </summary>
        /// <param name="value">Objeto a ser cadastrado</param>
        /// <returns>
        /// - 200 OK com o resultado do cadastro
        /// - 500 Bad Request em caso de erro.
        /// </returns>
        [HttpPost]
        public virtual async Task<IActionResult> Post([FromBody] TModel value)
        {
            _logger.LogInformation("Inserindo dados em {Entity} às {Time}", typeof(TEntity), DateTime.UtcNow);

            try
            {
                var result = await _service.AddAsync(value);

                if (!result)
                {
                    return BadRequest("Erro ao cadastrar registro.");
                }

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao cadastrar registro.");
                return BadRequest("Erro ao cadastrar registro.");
            }
        }

        /// <summary>
        /// Atualiza valores em um registro.
        /// </summary>
        /// <param name="value">Objeto a ser atualizado</param>
        /// <returns>
        /// - 200 OK com o resultado do cadastro
        /// - 204 No Content se não encontrar o registro
        /// - 500 Bad Request em caso de erro.
        /// </returns>
        [HttpPut]
        public virtual async Task<IActionResult> Put([FromBody] Guid id, [FromBody] TModel value)
        {
            _logger.LogInformation("Atualizando dados de {Entity} às {Time}", typeof(TEntity), DateTime.UtcNow);

            try
            {
                var result = await _service.UpdateAsync(id, value);

                return !result ? NoContent() : Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar registro.");
                return BadRequest("Erro ao atualizar registro.");
            }
        }

        /// <summary>
        /// Apaga um registro.
        /// </summary>
        /// <param name="id">Id do registro a ser apagado</param>
        /// <returns>
        /// - 200 OK com o resultado do cadastro
        /// - 204 No Content se não encontrar o registro
        /// - 500 Bad Request em caso de erro.
        /// </returns>
        [HttpDelete]
        public virtual async Task<IActionResult> Delete([FromBody] Guid id)
        {
            _logger.LogInformation("Apagando dados de {Entity} às {Time} com id {id}", typeof(TEntity), DateTime.UtcNow, id);

            try
            {
                var result = await _service.DeleteAsync(id);

                return !result ? NoContent() : Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao remover registro.");
                return BadRequest("Erro ao remover registro.");
            }
        }
    }
}