using Precificador.Domain.Entities.Base;

namespace Precificador.Domain.Entities
{
    public class MateriaPrima : CrudBase
    {
        public MateriaPrima(string nome, decimal qtdPacote, decimal vlrPacote, DateTime dataPreco, Guid grupoId, Guid unidadeMedidaId)
        {
            Nome = nome;
            QtdPacote = qtdPacote;
            VlrPacote = vlrPacote;
            DataPreco = dataPreco;
            GrupoId = grupoId;
            UnidadeMedidaId = unidadeMedidaId;
            AtualizarVlrUnitario();
        }

        public string Nome { get; private set; }
        public decimal QtdPacote { get; private set; }
        public decimal VlrPacote { get; private set; }
        public DateTime DataPreco { get; private set; }
        public decimal VlrUnitario { get; private set; }

        public Guid GrupoId { get; private set; }
        public Grupo? Grupo { get; private set; }
        public Guid UnidadeMedidaId { get; private set; }
        public UnidadeMedida? UnidadeMedida { get; }

        public ICollection<ProdutoMateriaPrima>? Produtos { get; }

        public void SetNome(string nome) => Nome = nome;
        public void SetQtdPacote(decimal qtdPacote) => QtdPacote = qtdPacote;
        public void SetVlrPacote(decimal vlrPacote) => VlrPacote = vlrPacote;
        public void SetDataPreco(DateTime dataPreco) => DataPreco = dataPreco;
        public void SetGrupoId(Guid grupoId) => GrupoId = grupoId;
        public void SetUnidadeMedidaId(Guid unidadeMedidaId) => UnidadeMedidaId = unidadeMedidaId;
        private void SetVlrUnitario(decimal vlrUnitario) => VlrUnitario = vlrUnitario;
        public void AtualizarVlrUnitario() => SetVlrUnitario(VlrPacote / QtdPacote);
    }
}