namespace AzureSummary.Models;

public enum BuildStatus
{
    Unknown,
    None,
    NotStarted,
    InProgress,
    Completed,
    Cancelling,
    Postponed
}

public enum BuildResult
{
    None,
    Succeeded,
    PartiallySucceeded,
    Failed,
    Canceled
}
