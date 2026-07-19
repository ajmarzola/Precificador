namespace Precificador.Domain.Entities;

public sealed class GlobalCostEntry
{
    public int GlobalCostEntryId { get; set; }
    public int GlobalCostGroupId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal? MonthlyAmount { get; set; }
    public string? Notes { get; set; }
    public string? SourceSheetName { get; set; }
    public int? SourceRowNumber { get; set; }
}
