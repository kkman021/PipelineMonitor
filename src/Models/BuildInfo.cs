namespace AzureSummary.Models;

public class BuildInfo
{
    public int BuildId { get; set; }
    public int DefinitionId { get; set; }
    public string DefinitionName { get; set; } = "";
    public BuildStatus Status { get; set; }
    public BuildResult Result { get; set; }
    public string? RequestedBy { get; set; }
    public DateTime? QueueTime { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? FinishTime { get; set; }
    public string? SourceBranch { get; set; }
    public string? WebUrl { get; set; }
    public StageProgress? Stages { get; set; }
}
