using Precificador.Application.Model.Base;
using System.ComponentModel.DataAnnotations;

namespace Precificador.Application.Model
{
    public class Produto : ModelBase
    {
        [Required(AllowEmptyStrings = false, ErrorMessage = "Necessário Informar o Nome do Produto"), MaxLength(200, ErrorMessage = "Nome do Produto deve ter no máximo 200 caracteres")]
        public required string Nome { get; set; }

        [Required(AllowEmptyStrings = false, ErrorMessage = "Necessário Informar o Id da Coleção")]
        public Guid ColecaoId { get; set; }

        [Required(AllowEmptyStrings = false, ErrorMessage = "Necessário Informar a Margem de Lucro"), Range(0.01, double.MaxValue, ErrorMessage = "Margem de Lucro deve ser maior que zero")]
        public decimal Margem { get; set; }

        public DateTime DataCalculoPreco { get; set; }

        public decimal PrecoCusto { get; set; }
        public decimal PrecoFinal { get; set; }
        public decimal PrecoCustoX3 { get; set; }
        public decimal PrecoCustoX35 { get; set; }
        public decimal PrecoCustoX4 { get; set; }
        public decimal PrecoVenda { get; set; }
        public decimal PrecoPromocional { get; set; }
    }
}