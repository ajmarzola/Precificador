using Precificador.Application.Model.Base;
using System.ComponentModel.DataAnnotations;

namespace Precificador.Application.Model
{
    public class MateriaPrima : ModelBase
    {
        [Required(AllowEmptyStrings = false, ErrorMessage = "Necessário Informar o Nome da Materia Prima"), MaxLength(200, ErrorMessage = "Nome da Materia Prima deve ter no máximo 200 caracteres")]
        public required string Nome { get; set; }

        [Required(AllowEmptyStrings = false, ErrorMessage = "Necessário Informar o Nome da Materia Prima"), Range(0.01, double.MaxValue, ErrorMessage = "Quantidade do Pacote deve ser maior que zero")]
        public decimal QtdPacote { get; set; }

        [Required(AllowEmptyStrings = false, ErrorMessage = "Necessário Informar o Valor da Matéria Prima"), Range(0.01, double.MaxValue, ErrorMessage = "Valor da Matéria Prima deve ser maior que zero")]
        public decimal VlrPacote { get; set; }

        public DateTime DataPreco { get; set; }

        public decimal VlrUnitario { get; set; }

        [Required(AllowEmptyStrings = false, ErrorMessage = "Necessário Informar o Id da Unidade de Medida")]
        public Guid UnidadeMedidaId { get; set; }

        [Required(AllowEmptyStrings = false, ErrorMessage = "Necessário Informar o Id do Grupo")]
        public Guid GrupoId { get; set; }
    }
}