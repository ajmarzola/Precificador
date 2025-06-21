using Precificador.Domain.Entities.Base;

namespace Precificador.Domain.Entities
{
    public class Produto : CrudBase
    {
        public required string Nome { get; set; }
        public decimal Margem { get; set; }
        public DateTime DataCalculoPreco { get; set; }
        public decimal PrecoCusto { get; set; }
        public decimal PrecoFinal { get; set; }
        public decimal PrecoVenda { get; set; }
        public decimal PrecoPromocional { get; set; }
        public decimal PrecoCustoX3 { get { return PrecoCusto * 3; } }
        public decimal PrecoCustoX35 { get { return PrecoCusto * 3.5m; } }
        public decimal PrecoCustoX4 { get { return PrecoCusto * 4; } }
        
        public ICollection<ColecaoProduto>? ColecaoProduto { get; }
        public ICollection<ProdutoMateriaPrima>? MateriasPrimas { get; }
        public ICollection<PesquisaPreco>? Pesquisas { get; }
    }
}