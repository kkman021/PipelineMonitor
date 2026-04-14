namespace AzureSummary.Models;

public class PipelineState
{
    public required PipelineEntry Entry { get; init; }
    public BuildInfo? CurrentBuild { get; set; }
    public BuildInfo? PreviousBuild { get; set; }
    public DateTime? LastPolledAt { get; set; }
    public string? LastError { get; set; }

    public bool HasStatusChanged
    {
        get
        {
            if (PreviousBuild is null || CurrentBuild is null) return false;
            return PreviousBuild.Status != CurrentBuild.Status
                || PreviousBuild.Result != CurrentBuild.Result
                || PreviousBuild.BuildId != CurrentBuild.BuildId;
        }
    }
}
