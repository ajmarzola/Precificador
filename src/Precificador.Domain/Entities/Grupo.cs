using Precificador.Domain.Entities.Base;

namespace Precificador.Domain.Entities
{
    public class Grupo(string nome) : CrudBase
    {
        public string Nome { get; private set; } = nome;
        public ICollection<MateriaPrima>? MateriasPrimas { get; }
        public void SetNome(string nome) => Nome = nome;
    }
}