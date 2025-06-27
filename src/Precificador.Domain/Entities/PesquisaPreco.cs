using Precificador.Domain.Entities.Base;

namespace Precificador.Domain.Entities
{
    public class PesquisaPreco(Guid produtoId, string local, decimal valor) : CrudBase
    {
        public Guid ProdutoId { get; private set; } = produtoId;
        public Produto? Produto { get; private set; }
        public string Local { get; private set; } = local;
        public decimal Valor { get; private set; } = valor;
        public DateTime DataPesquisa { get; private set; } = DateTime.Now;

        public void SetProdutoId(Guid produtoId) { ProdutoId = produtoId; }
        public void SetLocal(string local) { Local = local; }
        public void SetValor(decimal valor) { Valor = valor; }
    }
}