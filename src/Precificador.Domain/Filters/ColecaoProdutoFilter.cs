namespace Precificador.Domain.Filters
{
    public class ColecaoProdutoFilter : IFilter
    {
        public Guid? ColecaoId { get; set; }
        public string? ColecaoNome { get; set; }
        public Guid? ProdutoId { get; set; }
        public string? ProdutoNome { get; set; }

        public bool IsApplied()
        {
            return ColecaoId.HasValue || !string.IsNullOrEmpty(ColecaoNome) || ProdutoId.HasValue || !string.IsNullOrEmpty(ProdutoNome);
        }
    }
}
