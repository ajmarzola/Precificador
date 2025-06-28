using Precificador.Application.Services.Base;
using Precificador.Domain.Filters;
using Precificador.Domain.Repository;

namespace Precificador.Application.Services
{
    public class ColecaoProdutoService(IColecaoProdutoRepository repository) : CrudServiceBase<Model.ColecaoProduto, Domain.Entities.ColecaoProduto, ColecaoProdutoFilter, IColecaoProdutoRepository>(repository), IColecaoProdutoService
    {
        protected override Domain.Entities.ColecaoProduto ConvertToEntity(Model.ColecaoProduto model)
        {
            return new Domain.Entities.ColecaoProduto(model.ColecaoId, model.ProdutoId);
        }

        protected override Model.ColecaoProduto ConvertToModel(Domain.Entities.ColecaoProduto entity)
        {
            return new Model.ColecaoProduto
            {
                Id = entity.Id,
                ColecaoId = entity.ColecaoId,
                ProdutoId = entity.ProdutoId
            };
        }

        protected override void UpdateEntityFromModel(Domain.Entities.ColecaoProduto entity, Model.ColecaoProduto model)
        {
            entity.SetColecaoId(model.ColecaoId);
            entity.SetProdutoId(model.ProdutoId);
        }
    }
}