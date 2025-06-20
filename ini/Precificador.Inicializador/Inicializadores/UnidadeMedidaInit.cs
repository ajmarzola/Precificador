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
            new UnidadeMedida { Nome = "Folha 1/4 A4", Abreviacao = "1/4A4" },
            new UnidadeMedida { Nome = "Folha A4", Abreviacao = "FA4" },
            new UnidadeMedida { Nome = "Folha A5", Abreviacao = "FA5" },
            new UnidadeMedida { Nome = "Folha A6", Abreviacao = "FA6" },
            new UnidadeMedida { Nome = "Litro", Abreviacao = "l" },
            new UnidadeMedida { Nome = "Pacote", Abreviacao = "Pct" },
            new UnidadeMedida { Nome = "caixa", Abreviacao = "cx" },
            new UnidadeMedida { Nome = "folha A4     ", Abreviacao = "folha A4     " },
            new UnidadeMedida { Nome = "gramas", Abreviacao = "g" },
            new UnidadeMedida { Nome = "metros", Abreviacao = "m'      " },
            new UnidadeMedida { Nome = "mililitro", Abreviacao = "ml" },
            new UnidadeMedida { Nome = "Mini", Abreviacao = "mini" },
            new UnidadeMedida { Nome = "Unidade", Abreviacao = "Unid" },
            new UnidadeMedida { Nome = "Unidade 30cm ", Abreviacao = "U30 " },
        ];

        protected override NomeFilter GetFilter(UnidadeMedida item) => new() { Nome = item.Nome };

        protected override string BuildBody(UnidadeMedida item) => $"{{\"nome\": \"{item.Nome}\", \"abreviacao\": \"{item.Abreviacao}\"}}";
    }
}