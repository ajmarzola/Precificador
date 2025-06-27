using Precificador.Application.Model.Base;
using System.ComponentModel.DataAnnotations;

namespace Precificador.Application.Model
{
    public class PesquisaPreco : ModelBase
    {
        [Required(AllowEmptyStrings = false, ErrorMessage = "Necessário Informar o Id do Produto")]
        public Guid ProdutoId { get; set; }

        [Required(AllowEmptyStrings = false, ErrorMessage = "Necessário Informar o Local"), MaxLength(200, ErrorMessage = "Local deve ter no máximo 200 caracteres")]
        public required string Local { get; set; }

        [Required(AllowEmptyStrings = false, ErrorMessage = "Necessário Informar o Valor do Produto"), Range(0.01, double.MaxValue, ErrorMessage = "Valor do Produto deve ser maior que zero")]
        public decimal Valor { get; set; }

        [Required(AllowEmptyStrings = false, ErrorMessage = "Necessário Informar a Data da Pesquisa"), CustomValidation(typeof(PesquisaPreco), nameof(ValidateDataPesquisa))]
        public DateTime DataPesquisa { get; set; }

        public static ValidationResult ValidateDataPesquisa(DateTime data) => data < DateTime.Now.AddMonths(-1) || data > DateTime.Now ? new ValidationResult("Data da Pesquisa não deve ser anterior a 1 mês") : ValidationResult.Success!;
    }
}