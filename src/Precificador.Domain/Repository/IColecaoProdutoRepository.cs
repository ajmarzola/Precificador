using Precificador.Domain.Entities;
using Precificador.Domain.Filters;
using Precificador.Domain.Repository.Base;

namespace Precificador.Domain.Repository
{
    public interface IColecaoProdutoRepository: ICrudRepository<ColecaoProduto, ColecaoProdutoFilter>
    {
    }
}