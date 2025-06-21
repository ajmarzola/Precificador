using Precificador.Application.Services.Base;
using Precificador.Domain.Filters;
using Precificador.Domain.Repository;

namespace Precificador.Application.Services
{
    public interface IColecaoProdutoService:ICrudService<Model.ColecaoProduto, Domain.Entities.ColecaoProduto, ColecaoProdutoFilter, IColecaoProdutoRepository>
    {
    }
}