using Precificador.Inicializador.Services;
using RestSharp;
using System.Text.Json;

namespace Precificador.Inicializador.Base
{
    public abstract class BaseInit<TModel, TFilter>
    {
        protected abstract string Endpoint { get; }

        protected abstract IEnumerable<TModel> Items { get; }

        protected abstract TFilter GetFilter(TModel item);

        public void Inicializar()
        {
            foreach (var item in Items)
            {
                if (!VerificaExistencia(GetFilter(item)))
                {
                    Incluir(item);
                }
            }
        }

        private void Incluir(TModel item)
        {
            Console.WriteLine(PrecificadorApiService.Incluir($"/api/{Endpoint}", JsonSerializer.Serialize(item)));
        }

        private bool VerificaExistencia(TFilter filtro)
        {
            RestResponse response = PrecificadorApiService.GetByFilter($"/api/{Endpoint}/ByFilter", JsonSerializer.Serialize(filtro));

            if (response.IsSuccessStatusCode)
            {
                return !string.IsNullOrEmpty(response.Content);
            }
            else
            {
                Console.WriteLine($"Erro ao verificar existência: {response.ErrorMessage}");
                return false;
            }
        }
    }
}