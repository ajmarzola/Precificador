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
            RestClient client = CreateRestClient();
            RestRequest request = CreateRequest($"/api/{Endpoint}", Method.Post, JsonSerializer.Serialize(item));
            RestResponse response = client.Execute(request);
            Console.WriteLine(response.Content);
        }

        private static RestRequest CreateRequest(string endpoint, Method method, string body)
        {
            var request = new RestRequest(endpoint, method);
            request.AddHeader("Content-Type", "application/json");
            request.AddStringBody(body, DataFormat.Json);
            return request;
        }

        private static RestClient CreateRestClient()
        {
            return new RestClient(new RestClientOptions("https://localhost:7013"));
        }

        private bool VerificaExistencia(TFilter filtro)
        {
            RestClient client = CreateRestClient();
            var request = CreateRequest($"/api/{Endpoint}/ByFilter", Method.Get, JsonSerializer.Serialize(new { filtro }));
            RestResponse response = client.Execute(request);

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