namespace Precificador.Domain.Entities;

public sealed class ProductMaterial
{
    public int ProductMaterialId { get; set; }
    public int ProductId { get; set; }
    public int RawMaterialId { get; set; }
    public decimal? QuantityUsed { get; set; }
    public decimal? UnitCostSnapshot { get; set; }
    public decimal? TotalCostSnapshot { get; set; }
    public int SortOrder { get; set; }
    public int? SourceRowNumber { get; set; }
}
