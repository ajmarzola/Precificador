namespace Precificador.Application.Models;

public sealed class ProductBlockModel
{
    public string ProductName { get; set; } = string.Empty;
    public decimal? SuggestedPrice { get; set; }
    public decimal? MaterialCost { get; set; }
    public decimal? FinalComputedCost { get; set; }
    public decimal? MarginPercent { get; set; }
    public decimal? MarginAmount { get; set; }
    public decimal? PriceWithMargin { get; set; }
    public string? MarginNotes { get; set; }
    public string? SourceSheetName { get; set; }
    public int StartRow { get; set; }
    public int EndRow { get; set; }
    public List<ProductBomItemModel> Items { get; } = new();
    public List<MarketResearchItemModel> MarketResearch { get; } = new();
    public List<ProductCostComponentItemModel> CostComponents { get; } = new();
}
