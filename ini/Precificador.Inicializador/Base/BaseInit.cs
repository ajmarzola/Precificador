using RestSharp;

namespace Precificador.Inicializador.Base
{
    public abstract class BaseInit<TModel>
    {
        protected abstract string Endpoint { get; }
        protected abstract IEnumerable<TModel> Items { get; }
        protected abstract string GetNome(TModel item);
        protected abstract string BuildBody(TModel item);

        public void Inicializar()
        {
            foreach (var item in Items)
            {
                if (!VerificaExistencia(GetNome(item)))
                {
                    Incluir(item);
                }
            }
        }

        private void Incluir(TModel item)
        {
            var options = new RestClientOptions("https://localhost:7013");
            var client = new RestClient(options);
            var request = new RestRequest($"/api/{Endpoint}", Method.Post);
            request.AddHeader("Content-Type", "application/json");
            request.AddStringBody(BuildBody(item), DataFormat.Json);
            RestResponse response = client.Execute(request);
            Console.WriteLine(response.Content);
        }

        private bool VerificaExistencia(string filtro)
        {
            var options = new RestClientOptions("https://localhost:7013");
            var client = new RestClient(options);
            var request = new RestRequest($"/api/{Endpoint}/ByFilter", Method.Get);
            request.AddHeader("Content-Type", "application/json");

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