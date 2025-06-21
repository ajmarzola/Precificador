using Precificador.Domain.Entities.Base;

namespace Precificador.Domain.Entities
{
    public class ColecaoProduto : CrudBase
    {
        public Guid ColecaoId { get; set; }
        public Colecao? Colecao { get; set; }
        public Guid ProdutoId { get; set; }
        public Produto? Produto { get; set; }
    }
}