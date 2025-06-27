using Precificador.Domain.Entities.Base;

namespace Precificador.Domain.Entities
{
    public class ColecaoProduto(Guid colecaoId, Guid produtoId) : CrudBase
    {
        public Guid ColecaoId { get; private set; } = colecaoId;
        public Colecao? Colecao { get; }
        public Guid ProdutoId { get; private set; } = produtoId;
        public Produto? Produto { get; }
        public void SetColecaoId(Guid colecaoId) => ColecaoId = colecaoId;
        public void SetProdutoId(Guid produtoId) => ProdutoId = produtoId;
    }
}