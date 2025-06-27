using Precificador.Application.Services.Base;
using Precificador.Domain.Filters;
using Precificador.Domain.Repository;

namespace Precificador.Application.Services
{
    public class ProdutoMateriaPrimaService(IProdutoMateriaPrimaRepository repository) : CrudServiceBase<Model.ProdutoMateriaPrima, Domain.Entities.ProdutoMateriaPrima, ProdutoMateriaPrimaFilter, IProdutoMateriaPrimaRepository>(repository), IProdutoMateriaPrimaService
    {
        protected override Domain.Entities.ProdutoMateriaPrima ConvertToEntity(Model.ProdutoMateriaPrima model)
        {
            var retorno = new Domain.Entities.ProdutoMateriaPrima(model.ProdutoId, model.MateriaPrimaId, model.Quantidade);
            retorno.SetId(model.Id);
            return retorno;
        }

        protected override Model.ProdutoMateriaPrima ConvertToModel(Domain.Entities.ProdutoMateriaPrima entity)
        {
            return new Model.ProdutoMateriaPrima
            {
                Id = entity.Id,
                ProdutoId = entity.ProdutoId,
                MateriaPrimaId = entity.MateriaPrimaId,
                Quantidade = entity.Quantidade
            };
        }

        protected override void UpdateEntityFromModel(Domain.Entities.ProdutoMateriaPrima entity, Model.ProdutoMateriaPrima model)
        {
            entity.SetProdutoId(model.ProdutoId);
            entity.SetMateriaPrimaId(model.MateriaPrimaId);
            entity.SetQuantidade(model.Quantidade);
        }
    }
}