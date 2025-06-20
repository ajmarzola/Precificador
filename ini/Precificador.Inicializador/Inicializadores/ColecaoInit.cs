using Precificador.Application.Model;
using Precificador.Domain.Filters;
using Precificador.Inicializador.Base;

namespace Precificador.Inicializador.Inicializadores
{
    public class ColecaoInit : BaseInit<Colecao, ColecaoFilter>
    {
        protected override string Endpoint => "Colecao";

        protected override IEnumerable<Colecao> Items => colecoes;

        private readonly List<Colecao> colecoes =
        [
            new Colecao { Nome = "Mulher 2025", Ano = 2025 },
            new Colecao { Nome = "Volta às Aulas 2025", Ano = 2025 },
            new Colecao { Nome = "Coleção 2025", Ano = 2025 },
            new Colecao { Nome = "Dininha", Ano = 2024 },
            new Colecao { Nome = "Kraft", Ano = 2022 },
            new Colecao { Nome = "Couro", Ano = 2024 },
            new Colecao { Nome = "Crochê", Ano = 2020 },
            new Colecao { Nome = "Bebê", Ano = 2020 },
            new Colecao { Nome = "Casamento", Ano = 2020 },
            new Colecao { Nome = "Trevo Saúde", Ano = 2020 },
            new Colecao { Nome = "Fofurinhas", Ano = 2022 },
            new Colecao { Nome = "Costura Criativa", Ano = 2017 },
            new Colecao { Nome = "Sacolas em Papel", Ano = 2020 },
            new Colecao { Nome = "Sacolas 60", Ano = 2020 },
            new Colecao { Nome = "Sacolas 40", Ano = 2020 },
            new Colecao { Nome = "Arraiá 2024", Ano = 2024 },
            new Colecao { Nome = "Mães 2024", Ano = 2024 },
            new Colecao { Nome = "Páscoa 2024", Ano = 2024 },
            new Colecao { Nome = "Mulher 2024", Ano = 2024 },
            new Colecao { Nome = "Volta às Aulas 2024", Ano = 2024 },
            new Colecao { Nome = "Coleção 2024", Ano = 2024 },
            new Colecao { Nome = "Pais 2023", Ano = 2023 },
            new Colecao { Nome = "Namorados 2023", Ano = 2023 },
            new Colecao { Nome = "Mães 2023", Ano = 2023 },
            new Colecao { Nome = "Páscoa 2023", Ano = 2023 },
            new Colecao { Nome = "Mulher 2023", Ano = 2023 },
            new Colecao { Nome = "Volta às Aulas23", Ano = 2023 },
            new Colecao { Nome = "Coleção 2023", Ano = 2023 },
            new Colecao { Nome = "Natal 2022", Ano = 2022 }
        ];

        protected override ColecaoFilter GetFilter(Colecao item) => new() { Nome = item.Nome, Ano = item.Ano };
    }
}