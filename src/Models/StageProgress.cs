namespace AzureSummary.Models;

public class StageProgress
{
    public int CurrentIndex { get; set; }   // 1-based, currently in-progress stage position
    public int Total { get; set; }
    public string? CurrentStageName { get; set; }
}
