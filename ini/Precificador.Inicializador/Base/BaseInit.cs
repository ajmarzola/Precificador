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
            var body = JsonSerializer.Serialize(item);
            var response = PrecificadorApiService.Incluir($"/api/{Endpoint}", body);

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Registro Incluido com Sucesso: {body}");
            }
            else
            {
                Console.WriteLine($"Erro ao incluir registro: {response.ErrorMessage}");
            }
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