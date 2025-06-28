using Precificador.Application.Services.Base;
using Precificador.Domain.Filters;
using Precificador.Domain.Repository;

namespace Precificador.Application.Services
{
    public class UnidadeMedidaService(IUnidadeMedidaRepository repository) : CrudServiceBase<Model.UnidadeMedida, Domain.Entities.UnidadeMedida, NomeFilter, IUnidadeMedidaRepository>(repository), IUnidadeMedidaService
    {
        protected override Domain.Entities.UnidadeMedida ConvertToEntity(Model.UnidadeMedida model)
        {
            return new Domain.Entities.UnidadeMedida(model.Nome, model.Abreviacao);
        }

        protected override Model.UnidadeMedida ConvertToModel(Domain.Entities.UnidadeMedida entity)
        {
            return new Model.UnidadeMedida
            {
                Id = entity.Id,
                Nome = entity.Nome,
                Abreviacao = entity.Abrebiacao
            };
        }

        protected override void UpdateEntityFromModel(Domain.Entities.UnidadeMedida entity, Model.UnidadeMedida model)
        {
            entity.SetNome(model.Nome);
            entity.SetAbrebiacao(model.Abreviacao);
        }
    }
}