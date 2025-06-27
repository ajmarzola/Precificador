using Precificador.Domain.Entities.Base;

namespace Precificador.Domain.Entities
{
    public class Produto(string nome, decimal margem, DateTime dataCalculoPreco, decimal precoCusto, decimal precoFinal, decimal precoVenda, decimal precoPromocional) : CrudBase
    {
        public string Nome { get; private set; } = nome;
        public decimal Margem { get; private set; } = margem;
        public DateTime DataCalculoPreco { get; private set; } = dataCalculoPreco;
        public decimal PrecoCusto { get; private set; } = precoCusto;
        public decimal PrecoFinal { get; private set; } = precoFinal;
        public decimal PrecoVenda { get; private set; } = precoVenda;
        public decimal PrecoPromocional { get; private set; } = precoPromocional;
        public decimal PrecoCustoX3 { get; private set; }
        public decimal PrecoCustoX35 { get; private set; }
        public decimal PrecoCustoX4 { get; private set; }

        public ICollection<ColecaoProduto>? ColecaoProduto { get; }
        public ICollection<ProdutoMateriaPrima>? MateriasPrimas { get; }
        public ICollection<PesquisaPreco>? Pesquisas { get; }

        public void SetNome(string nome) => Nome = nome;
        public void SetMargem(decimal margem) => Margem = margem;
        public void SetPrecoCusto(decimal precoCusto) => PrecoCusto = precoCusto;
        public void SetPrecoFinal(decimal precoFinal) => PrecoFinal = precoFinal;
        public void SetPrecoVenda(decimal precoVenda) => PrecoVenda = precoVenda;
        public void SetPrecoPromocional(decimal precoPromocional) => PrecoPromocional = precoPromocional;
        private void SetPrecoCustoX3(decimal precoCustoX3) => PrecoCustoX3 = precoCustoX3;
        private void SetPrecoCustoX35(decimal precoCustoX35) => PrecoCustoX35 = precoCustoX35;
        private void SetPrecoCustoX4(decimal precoCustoX4) => PrecoCustoX4 = precoCustoX4;
        public void AtualizarPrecoCustoX3() => SetPrecoCustoX3(PrecoCusto * 3);
        public void AtualizarPrecoCustoX35() => SetPrecoCustoX35(PrecoCusto * 3.5m);
        public void AtualizarPrecoCustoX4() => SetPrecoCustoX4(PrecoCusto * 4);
    }
}