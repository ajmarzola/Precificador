using Precificador.Application.Services.Base;
using Precificador.Domain.Filters;
using Precificador.Domain.Repository;

namespace Precificador.Application.Services
{
    public class GrupoService(IGrupoRepository repository) : CrudServiceBase<Model.Grupo, Domain.Entities.Grupo, NomeFilter, IGrupoRepository>(repository), IGrupoService
    {
        protected override Domain.Entities.Grupo ConvertToEntity(Model.Grupo model)
        {
            var retorno = new Domain.Entities.Grupo(model.Nome);
            retorno.SetId(model.Id);
            return retorno;
        }

        protected override Model.Grupo ConvertToModel(Domain.Entities.Grupo entity)
        {
            return new Model.Grupo
            {
                Id = entity.Id,
                Nome = entity.Nome
            };
        }

        protected override void UpdateEntityFromModel(Domain.Entities.Grupo entity, Model.Grupo model)
        {
            entity.SetNome(model.Nome);
        }
    }
}