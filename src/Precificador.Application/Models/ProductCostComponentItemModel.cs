namespace Precificador.Application.Models;

public sealed class ProductCostComponentItemModel
{
    public string TypeName { get; set; } = string.Empty;
    public decimal? Amount { get; set; }
    public decimal? Percentage { get; set; }
    public string? Notes { get; set; }
    public int SortOrder { get; set; }
}
