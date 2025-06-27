using Precificador.Domain.Entities.Base;

namespace Precificador.Domain.Entities
{
    public class Colecao(string nome, int ano, DateTime? dataLancamento) : CrudBase
    {
        public string Nome { get; private set; } = nome;
        public int Ano { get; private set; } = ano;
        public DateTime? DataLancamento { get; private set; } = dataLancamento;
        public ICollection<ColecaoProduto>? ColecaoProduto { get; }

        public void SetNome(string nome) => Nome = nome;
        public void SetAno(int ano) => Ano = ano;
        public void SetDataLancamento(DateTime? dataLancamento) => DataLancamento = dataLancamento;
    }
}