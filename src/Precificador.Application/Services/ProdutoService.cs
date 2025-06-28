using Precificador.Application.Services.Base;
using Precificador.Domain.Filters;
using Precificador.Domain.Repository;

namespace Precificador.Application.Services
{
    public class ProdutoService(IProdutoRepository repository) : CrudServiceBase<Model.Produto, Domain.Entities.Produto, NomeFilter, IProdutoRepository>(repository), IProdutoService
    {
        protected override Domain.Entities.Produto ConvertToEntity(Model.Produto model)
        {
            return new Domain.Entities.Produto(model.Nome, model.Margem, model.DataCalculoPreco, model.PrecoCusto, model.PrecoFinal, model.PrecoVenda, model.PrecoPromocional);
        }

        protected override Model.Produto ConvertToModel(Domain.Entities.Produto entity)
        {
            return new Model.Produto
            {
                Id = entity.Id,
                Nome = entity.Nome,
                Margem = entity.Margem,
                DataCalculoPreco = entity.DataCalculoPreco,
                PrecoCusto = entity.PrecoCusto,
                PrecoFinal = entity.PrecoFinal,
                PrecoCustoX3 = entity.PrecoCustoX3,
                PrecoCustoX35 = entity.PrecoCustoX35,
                PrecoCustoX4 = entity.PrecoCustoX4
            };
        }

        protected override void UpdateEntityFromModel(Domain.Entities.Produto entity, Model.Produto model)
        {
            entity.SetNome(model.Nome);
            entity.SetMargem(model.Margem);
            entity.SetPrecoCusto(model.PrecoCusto);
        }
    }
}