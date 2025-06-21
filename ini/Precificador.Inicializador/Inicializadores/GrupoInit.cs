using Precificador.Application.Model;
using Precificador.Domain.Filters;
using Precificador.Inicializador.Base;

namespace Precificador.Inicializador.Inicializadores
{
    public class GrupoInit : BaseInit<Grupo, NomeFilter>
    {
        protected override string Endpoint => "Grupo";

        protected override IEnumerable<Grupo> Items => grupos;

        private readonly List<Grupo> grupos =
        [
            new Grupo { Nome = "Acetato" },
            new Grupo { Nome = "Acrilico" },
            new Grupo { Nome = "Armarinhos" },
            new Grupo { Nome = "Caderno" },
            new Grupo { Nome = "Color Plus" },
            new Grupo { Nome = "Color Plus Aspen 120g" },
            new Grupo { Nome = "Contact" },
            new Grupo { Nome = "Couro" },
            new Grupo { Nome = "Custos Fixos" },
            new Grupo { Nome = "Desgaste" },
            new Grupo { Nome = "Doces" },
            new Grupo { Nome = "EVA" },
            new Grupo { Nome = "Embalagem" },
            new Grupo { Nome = "Envelopes" },
            new Grupo { Nome = "Espiral" },
            new Grupo { Nome = "Feltro" },
            new Grupo { Nome = "Fios" },
            new Grupo { Nome = "Fitas" },
            new Grupo { Nome = "Ima" },
            new Grupo { Nome = "Impressão" },
            new Grupo { Nome = "Laminação e Plastificação" },
            new Grupo { Nome = "Medalhas e Pingentes" },
            new Grupo { Nome = "Metais" },
            new Grupo { Nome = "Miçangas e Pedrarias" },
            new Grupo { Nome = "OffSet 120g" },
            new Grupo { Nome = "OffSet 180g" },
            new Grupo { Nome = "OffSet 240g" },
            new Grupo { Nome = "OffSet 75g" },
            new Grupo { Nome = "OffSet 90g" },
            new Grupo { Nome = "OffSet Adesivado" },
            new Grupo { Nome = "Offset 90g" },
            new Grupo { Nome = "Outros" },
            new Grupo { Nome = "Papel Fotográfico" },
            new Grupo { Nome = "Papel Kraft" },
            new Grupo { Nome = "Papel Pólen" },
            new Grupo { Nome = "Papel Transfer" },
            new Grupo { Nome = "Papel vegetal" },
            new Grupo { Nome = "Papelaria" },
            new Grupo { Nome = "Papelão Cinza" },
            new Grupo { Nome = "Plastico para encadernação" },
            new Grupo { Nome = "Plástico Cristal" },
            new Grupo { Nome = "Post It" },
            new Grupo { Nome = "Pró-labore" },
            new Grupo { Nome = "Tricoline" }
        ];

        protected override NomeFilter GetFilter(Grupo item) => new() { Nome = item.Nome };
    }
}