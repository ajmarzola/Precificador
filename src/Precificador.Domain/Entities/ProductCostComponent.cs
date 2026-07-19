namespace Precificador.Domain.Entities;

public sealed class ProductCostComponent
{
    public int ProductCostComponentId { get; set; }
    public int ProductId { get; set; }
    public int CostComponentTypeId { get; set; }
    public decimal? Amount { get; set; }
    public decimal? Percentage { get; set; }
    public string? Notes { get; set; }
    public int SortOrder { get; set; }
}
