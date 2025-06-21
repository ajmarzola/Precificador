using Precificador.Application.Model.Base;
using System.ComponentModel.DataAnnotations;

namespace Precificador.Application.Model
{
    public class ColecaoProduto:ModelBase
    {
        [Required(AllowEmptyStrings = false, ErrorMessage = "Necessário Informar o Id da Coleção")]
        public Guid ColecaoId { get; set; }

        [Required(AllowEmptyStrings = false, ErrorMessage = "Necessário Informar o Id do Produto")]
        public Guid ProdutoId { get; set; }
    }
}