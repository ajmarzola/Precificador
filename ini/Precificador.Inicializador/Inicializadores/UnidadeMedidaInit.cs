using Precificador.Application.Model;
using Precificador.Domain.Filters;
using Precificador.Inicializador.Base;

namespace Precificador.Inicializador.Inicializadores
{
    public class UnidadeMedidaInit : BaseInit<UnidadeMedida, NomeFilter>
    {
        protected override string Endpoint => "UnidadeMedida";

        protected override IEnumerable<UnidadeMedida> Items => unidadesMedida;

        private readonly List<UnidadeMedida> unidadesMedida =
        [
            new UnidadeMedida { Nome = "Caixa", Abreviacao = "cx" },
            new UnidadeMedida { Nome = "Folha 1/4 A4", Abreviacao = "1/4" },
            new UnidadeMedida { Nome = "Folha", Abreviacao = "fl" },
            new UnidadeMedida { Nome = "Folha A4", Abreviacao = "A4" },
            new UnidadeMedida { Nome = "Folha A5", Abreviacao = "A5" },
            new UnidadeMedida { Nome = "Folha A6", Abreviacao = "A6" },
            new UnidadeMedida { Nome = "gramas", Abreviacao = "g" },
            new UnidadeMedida { Nome = "Litro", Abreviacao = "L" },
            new UnidadeMedida { Nome = "metros", Abreviacao = "m" },
            new UnidadeMedida { Nome = "mililitro", Abreviacao = "mL" },
            new UnidadeMedida { Nome = "Pacote", Abreviacao = "pct" },
            new UnidadeMedida { Nome = "Unidade", Abreviacao = "unid" },
        ];

        protected override NomeFilter GetFilter(UnidadeMedida item) => new() { Nome = item.Nome };
    }
}