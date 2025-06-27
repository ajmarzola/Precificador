using Precificador.Application.Model.Base;
using System.ComponentModel.DataAnnotations;

namespace Precificador.Application.Model
{
    public class ProdutoMateriaPrima : ModelBase
    {
        [Required(AllowEmptyStrings = false, ErrorMessage = "Necessário Informar o Id do Produto")]
        public Guid ProdutoId { get; set; }

        [Required(AllowEmptyStrings = false, ErrorMessage = "Necessário Informar o Id da Matéria Prima")]
        public Guid MateriaPrimaId { get; set; }

        [Required(AllowEmptyStrings = false, ErrorMessage = "Necessário Informar a Quantidade Utilizada"), Range(0.01, double.MaxValue, ErrorMessage = "Quantidade Utilizada deve ser maior que zero")]
        public decimal Quantidade { get; set; }
    }
}