using Precificador.Application.Services.Base;
using Precificador.Domain.Filters;
using Precificador.Domain.Repository;

namespace Precificador.Application.Services
{
    public class ColecaoService(IColecaoRepository repository) : CrudServiceBase<Model.Colecao, Domain.Entities.Colecao, ColecaoFilter, IColecaoRepository>(repository), IColecaoService
    {
        protected override Domain.Entities.Colecao ConvertToEntity(Model.Colecao model)
        {
            var retorno = new Domain.Entities.Colecao(model.Nome, model.Ano, model.DataLancamento);
            retorno.SetId(model.Id);
            return retorno;
        }

        protected override Model.Colecao ConvertToModel(Domain.Entities.Colecao entity)
        {
            return new Model.Colecao
            {
                Id = entity.Id,
                Nome = entity.Nome,
                Ano = entity.Ano,
                DataLancamento = entity.DataLancamento
            };
        }

        protected override void UpdateEntityFromModel(Domain.Entities.Colecao entity, Model.Colecao model)
        {
            entity.SetNome(model.Nome);
            entity.SetAno(model.Ano);
            entity.SetDataLancamento(model.DataLancamento);
        }
    }
}