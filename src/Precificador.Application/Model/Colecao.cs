using Precificador.Application.Model.Base;
using System.ComponentModel.DataAnnotations;

namespace Precificador.Application.Model
{
    public class Colecao : ModelBase
    {
        [Required(AllowEmptyStrings = false, ErrorMessage = "Necessário Informar o Nome da Coleção"), MaxLength(100, ErrorMessage = "Nome da Coleção deve ter no máximo 100 caracteres")]
        public required string Nome { get; set; }

        [Required(AllowEmptyStrings = false, ErrorMessage = "Necessário Informar o Ano de Lançamento"), CustomValidation(typeof(Colecao), nameof(ValidateAno))]
        public int Ano { get; set; }

        [CustomValidation(typeof(Colecao), nameof(ValidateDataLancamento))]
        public DateTime? DataLancamento { get; set; }

        public static ValidationResult ValidateAno(int ano) => ano < 2016 || ano > DateTime.Now.Year ? new ValidationResult("Ano de Lançamento deve estar entre 2016 e o ano atual") : ValidationResult.Success!;

        public static ValidationResult ValidateDataLancamento(DateTime? data) => (data != null) && (data < DateTime.Parse("2016-01-12")) ? new ValidationResult("Data de Lançamento deve ser posterior à criação da loja") : ValidationResult.Success!;
    }
}