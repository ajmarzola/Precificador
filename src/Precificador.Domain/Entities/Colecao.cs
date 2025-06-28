using Precificador.Domain.Entities.Base;

namespace Precificador.Domain.Entities
{
    public class Colecao(string nome, int ano, DateTime? dataLancamento) : CrudBase
    {
        public string Nome { get; private set; } = nome;
        public int Ano { get; private set; } = ano;
        public DateTime? DataLancamento { get; private set; } = dataLancamento;
        public ICollection<ColecaoProduto>? ColecaoProduto { get; }

        public void SetNome(string nome)
        {
            if (string.IsNullOrWhiteSpace(nome))
            {
                throw new ArgumentException("Nome não pode ser vazio ou nulo.", nameof(nome));
            }

            Nome = nome;
        }

        public void SetAno(int ano)
        {
            if (ano < 2016)
            {
                throw new ArgumentOutOfRangeException(nameof(ano), "Ano deve ser maior ou igual a 2016.");
            }

            Ano = ano;
        }

        public void SetDataLancamento(DateTime? dataLancamento)
        {
            if (dataLancamento.HasValue && dataLancamento.Value < new DateTime(2016, 01, 12))
            {
                throw new ArgumentOutOfRangeException(nameof(dataLancamento), "Data de lançamento não pode ser anterior a 12/01/2016.");
            }

            DataLancamento = dataLancamento;
        }
    }
}