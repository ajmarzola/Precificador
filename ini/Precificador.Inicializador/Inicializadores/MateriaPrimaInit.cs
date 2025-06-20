using Precificador.Application.Model;
using Precificador.Domain.Filters;
using Precificador.Inicializador.Base;

namespace Precificador.Inicializador.Inicializadores
{
    public class MateriaPrimaInit : BaseInit<MateriaPrima, NomeFilter>
    {
        protected override string Endpoint => "MateriaPrima";

        protected override IEnumerable<MateriaPrima> Items => materiasPrimas;

        private static IEnumerable<MateriaPrima> materiasPrimas =>
        [
            new() {
                Nome = "Farinha de Trigo",
                QtdPacote = 25,
                VlrPacote = 50.00m,
                DataPreco = DateTime.Now,
                VlrUnitario = 2.00m,
                UnidadeMedidaId = Guid.Parse("00000000-0000-0000-0000-000000000001"), // Exemplo de GUID
                GrupoId = Guid.Parse("00000000-0000-0000-0000-000000000002") // Exemplo de GUID
            },
            new() {
                Nome = "Açúcar",
                QtdPacote = 10,
                VlrPacote = 20.00m,
                DataPreco = DateTime.Now,
                VlrUnitario = 2.00m,
                UnidadeMedidaId = Guid.Parse("00000000-0000-0000-0000-000000000003"), // Exemplo de GUID
                GrupoId = Guid.Parse("00000000-0000-0000-0000-000000000004") // Exemplo de GUID
            }
        ];

        protected override NomeFilter GetFilter(MateriaPrima item) => new() { Nome = item.Nome };
    }
}