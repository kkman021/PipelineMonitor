namespace AzureSummary.Models;

public class PipelineEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Organization { get; set; } = "";
    public string Project { get; set; } = "";
    public int DefinitionId { get; set; }
    public string? DisplayName { get; set; }
    public string? OverridePat { get; set; }
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
}
