using Precificador.Inicializador.Inicializadores;

namespace Precificador.Inicializador
{
    public class Program
    {
        public static void Main()
        {
            new GrupoInit().Inicializar();
            new UnidadeMedidaInit().Inicializar();
            new MateriaPrimaInit().Inicializar();
            
            new ColecaoInit().Inicializar();
        }
    }
}