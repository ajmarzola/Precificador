namespace Precificador.Domain.Entities;

public sealed class Collection
{
    public int CollectionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? SourceSheetName { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
