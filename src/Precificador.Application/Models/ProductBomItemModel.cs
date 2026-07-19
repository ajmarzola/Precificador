namespace Precificador.Application.Models;

public sealed class ProductBomItemModel
{
    public string MaterialName { get; set; } = string.Empty;
    public decimal? QuantityUsed { get; set; }
    public decimal? UnitCost { get; set; }
    public decimal? TotalCost { get; set; }
    public int SourceRowNumber { get; set; }
}
