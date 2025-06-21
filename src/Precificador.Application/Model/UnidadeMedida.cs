using Precificador.Application.Model.Base;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Precificador.Application.Model
{
    public class UnidadeMedida : ModelBase
    {
        [Required(AllowEmptyStrings = false, ErrorMessage = "Necessário Informar o Nome da Unidade de Medida")]
        [MaxLength(100, ErrorMessage = "Nome do Grupo deve ter no máximo 100 caracteres")]
        [JsonPropertyName("nome")]
        public required string Nome { get; set; }

        [Required(AllowEmptyStrings = false, ErrorMessage = "Necessário Informar a Abreviação da Unidade de Medida")]
        [MaxLength(4, ErrorMessage = "Abreviação deve ter no máximo 4 caracteres")]
        [JsonPropertyName("abreviacao")]
        public required string Abreviacao { get; set; }
    }
}