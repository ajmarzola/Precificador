using Precificador.Application.Model.Base;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Precificador.Application.Model
{
    public class Grupo : ModelBase
    {
        [Required(AllowEmptyStrings = false, ErrorMessage = "Necessário Informar o Nome do Grupo")]
        [MaxLength(100, ErrorMessage = "Nome do Grupo deve ter no máximo 100 caracteres")]
        [JsonPropertyName("nome")]
        public required string Nome { get; set; }
    }
}