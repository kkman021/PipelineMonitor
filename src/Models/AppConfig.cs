namespace AzureSummary.Models;

public class AppConfig
{
    public string? GlobalPat { get; set; }
    public List<PipelineEntry> Pipelines { get; set; } = [];
    public int PollingIntervalSeconds { get; set; } = 300;
    public List<DashboardColumn> VisibleColumns { get; set; } =
    [
        DashboardColumn.Pipeline,
        DashboardColumn.Build,
        DashboardColumn.Branch,
        DashboardColumn.Status,
        DashboardColumn.Stages,
        DashboardColumn.Result,
        DashboardColumn.Duration,
        DashboardColumn.LastPoll
    ];
}
