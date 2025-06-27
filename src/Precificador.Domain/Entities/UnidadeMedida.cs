using Precificador.Domain.Entities.Base;

namespace Precificador.Domain.Entities
{
    public class UnidadeMedida(string nome, string abrebiacao) : CrudBase
    {
        public string Nome { get; private set; } = nome;
        public string Abrebiacao { get; private set; } = abrebiacao;

        public void SetNome(string nome) => Nome = nome;
        public void SetAbrebiacao(string abrebiacao) => Abrebiacao = abrebiacao;

    }
}