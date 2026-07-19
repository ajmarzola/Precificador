namespace Precificador.Domain.Entities;

public sealed class RawMaterial
{
    public int RawMaterialId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal? PackageQuantity { get; set; }
    public decimal? PackagePrice { get; set; }
    public decimal? FreightAmount { get; set; }
    public string? UnitOfMeasure { get; set; }
    public decimal? UnitCost { get; set; }
    public decimal? FreightUnitCost { get; set; }
    public decimal? TotalUnitCost { get; set; }
}
