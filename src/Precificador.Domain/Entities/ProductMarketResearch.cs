namespace Precificador.Domain.Entities;

public sealed class ProductMarketResearch
{
    public int ProductMarketResearchId { get; set; }
    public int ProductId { get; set; }
    public string Label { get; set; } = string.Empty;
    public decimal? ObservedPrice { get; set; }
    public string? Notes { get; set; }
    public int SortOrder { get; set; }
}
