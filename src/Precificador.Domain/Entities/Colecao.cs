using Precificador.Domain.Entities.Base;

namespace Precificador.Domain.Entities
{
    public class Colecao : CrudBase
    {
        public required string Nome { get; set; }
        public int Ano { get; set; }
        public DateTime? DataLancamento { get; set; }
        public ICollection<ColecaoProduto>? ColecaoProduto { get; }
    }
}