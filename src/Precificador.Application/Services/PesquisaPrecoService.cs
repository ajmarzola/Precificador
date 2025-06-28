using Precificador.Application.Services.Base;
using Precificador.Domain.Filters;
using Precificador.Domain.Repository;

namespace Precificador.Application.Services
{
    public class PesquisaPrecoService(IPesquisaPrecoRepository repository) : CrudServiceBase<Model.PesquisaPreco, Domain.Entities.PesquisaPreco, PesquisaPrecoFilter, IPesquisaPrecoRepository>(repository), IPesquisaPrecoService
    {
        protected override Domain.Entities.PesquisaPreco ConvertToEntity(Model.PesquisaPreco model)
        {
            return new Domain.Entities.PesquisaPreco(model.ProdutoId, model.Local, model.Valor);
        }

        protected override Model.PesquisaPreco ConvertToModel(Domain.Entities.PesquisaPreco entity)
        {
            return new Model.PesquisaPreco
            {
                Id = entity.Id,
                ProdutoId = entity.ProdutoId,
                Local = entity.Local,
                Valor = entity.Valor,
                DataPesquisa = entity.DataPesquisa
            };
        }

        protected override void UpdateEntityFromModel(Domain.Entities.PesquisaPreco entity, Model.PesquisaPreco model)
        {
            entity.SetProdutoId(model.ProdutoId);
            entity.SetLocal(model.Local);
            entity.SetValor(model.Valor);
        }
    }
}