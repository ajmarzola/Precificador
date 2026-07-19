namespace Precificador.Application.Models;

public sealed class MarketResearchItemModel
{
    public string Label { get; set; } = string.Empty;
    public decimal? ObservedPrice { get; set; }
    public string? Notes { get; set; }
}
