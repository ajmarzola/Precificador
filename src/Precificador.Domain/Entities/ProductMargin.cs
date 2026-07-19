namespace Precificador.Domain.Entities;

public sealed class ProductMargin
{
    public int ProductMarginId { get; set; }
    public int ProductId { get; set; }
    public decimal? MarginPercent { get; set; }
    public decimal? MarginAmount { get; set; }
    public decimal? PriceWithMargin { get; set; }
    public string? Notes { get; set; }
}
