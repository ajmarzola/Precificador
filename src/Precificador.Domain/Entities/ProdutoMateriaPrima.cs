using Precificador.Domain.Entities.Base;

namespace Precificador.Domain.Entities
{
    public class ProdutoMateriaPrima(Guid produtoId, Guid materiaPrimaId, decimal quantidade) : CrudBase
    {
        public Guid ProdutoId { get; private set; } = produtoId;
        public Produto? Produto { get; }
        public Guid MateriaPrimaId { get; private set; } = materiaPrimaId;
        public MateriaPrima? MateriaPrima { get; }
        public decimal Quantidade { get; private set; } = quantidade;

        public void SetProdutoId(Guid produtoId) => ProdutoId = produtoId;
        public void SetMateriaPrimaId(Guid materiaPrimaId) => MateriaPrimaId = materiaPrimaId;
        public void SetQuantidade(decimal quantidade) => Quantidade = quantidade;
    }
}