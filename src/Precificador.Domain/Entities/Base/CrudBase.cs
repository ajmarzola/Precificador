using System.ComponentModel.DataAnnotations;

namespace Precificador.Domain.Entities.Base
{
    //TODO: Definir Objetos de valor para nome, ano, data e valor monetário
    //O objeto de valor é para entidade, os datamodel são anemicos

    public abstract class CrudBase
    {
        [Key]
        public Guid Id { get; private set; }
        public DateTime DataCriacao { get; private set; }
        public DateTime? DataAlteracao { get; private set; }
        public bool Ativo { get; private set; }

        protected CrudBase() => Id = Guid.NewGuid();

        public void SetId(Guid id) => Id = id;
        public void SetDataCriacao() => DataCriacao = DateTime.Now;
        public void SetDataAlterado() => DataAlteracao = DateTime.Now;
        private void SetAtivo(bool ativo) => Ativo = ativo;
        public void Ativar() => SetAtivo(true);
        public void Inativar() => SetAtivo(false);
    }
}