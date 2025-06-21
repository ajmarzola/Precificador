using System.Text.Json.Serialization;

namespace Precificador.Application.Model.Base
{
    public abstract class ModelBase
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }
    }
}