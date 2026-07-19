namespace Precificador.Domain.Entities;

public sealed class Product
{
    public int ProductId { get; set; }
    public int CollectionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal? SuggestedPrice { get; set; }
    public decimal? MaterialCostTotal { get; set; }
    public decimal? FinalComputedCost { get; set; }
    public string? SourceSheetName { get; set; }
    public int? SourceRowStart { get; set; }
    public int? SourceRowEnd { get; set; }
}
