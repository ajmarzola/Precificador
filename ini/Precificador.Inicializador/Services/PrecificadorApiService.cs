using RestSharp;

namespace Precificador.Inicializador.Services
{
    public class PrecificadorApiService
    {
        public static RestResponse Incluir(string endpoint, string jsonBody)
        {
            RestClient client = CreateRestClient();
            RestRequest request = CreateRequest(endpoint, Method.Post, jsonBody);
            return client.Execute(request);
        }

        public static RestResponse GetByFilter(string endpoint, string jsonBody) {

            RestClient client = CreateRestClient();
            var request = CreateRequest(endpoint, Method.Get, jsonBody);
            return client.Execute(request);
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
            return new RestClient(new RestClientOptions("http://localhost:8080"));
        }
    }
}
