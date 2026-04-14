using AzureSummary.Models;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace AzureSummary.Display;

public static class StatusFormatter
{
    public static IRenderable[] FormatRow(PipelineState state, IReadOnlyList<DashboardColumn> visibleColumns)
    {
        var build = state.CurrentBuild;
        var entry = state.Entry;
        var cells = new List<IRenderable>();

        foreach (var col in visibleColumns)
        {
            cells.Add(col switch
            {
                DashboardColumn.Pipeline =>
                    new Markup(Markup.Escape(entry.DisplayName ?? build?.DefinitionName ?? $"ID:{entry.DefinitionId}")),
                DashboardColumn.Organization =>
                    new Markup(Markup.Escape(entry.Organization)),
                DashboardColumn.Project =>
                    new Markup(Markup.Escape(entry.Project)),
                DashboardColumn.Build =>
                    FormatBuildLink(build),
                DashboardColumn.Branch =>
                    FormatBranch(build?.SourceBranch),
                DashboardColumn.Status =>
                    FormatStatus(build?.Status, state.LastError),
                DashboardColumn.Stages =>
                    FormatStages(build),
                DashboardColumn.Result =>
                    FormatResult(build?.Result),
                DashboardColumn.Duration =>
                    FormatDuration(build),
                DashboardColumn.TriggeredBy =>
                    new Markup(Markup.Escape(build?.RequestedBy ?? "-")),
                DashboardColumn.LastPoll =>
                    FormatLastUpdated(state),
                _ => new Markup("")
            });
        }

        return [.. cells];
    }

    private static IRenderable FormatBuildLink(BuildInfo? build)
    {
        if (build is null) return new Markup("[dim]-[/]");
        if (build.WebUrl is null) return new Markup($"[dim]#{build.BuildId}[/]");
        return new Markup($"[link={build.WebUrl}]#{build.BuildId}[/]");
    }

    private static Markup FormatBranch(string? branch)
    {
        if (branch is null) return new Markup("[dim]-[/]");
        var display = branch.StartsWith("refs/heads/", StringComparison.Ordinal)
            ? branch["refs/heads/".Length..]
            : branch;
        return new Markup(Markup.Escape(display));
    }

    private static Markup FormatStatus(BuildStatus? status, string? error)
    {
        if (error is not null)
            return new Markup("[red]ERR[/]");

        return status switch
        {
            BuildStatus.InProgress => new Markup("[yellow]Running[/]"),
            BuildStatus.Completed => new Markup("[dim]Done[/]"),
            BuildStatus.Cancelling => new Markup("[orange3]Cancelling[/]"),
            BuildStatus.Postponed => new Markup("[dim]Postponed[/]"),
            BuildStatus.NotStarted => new Markup("[dim]Queued[/]"),
            BuildStatus.None => new Markup("[dim]-[/]"),
            null => new Markup("[dim]-[/]"),
            _ => new Markup(status.ToString()!)
        };
    }

    private static Markup FormatStages(BuildInfo? build)
    {
        if (build is null || build.Status != BuildStatus.InProgress)
            return new Markup("[dim]-[/]");

        if (build.Stages is null)
            return new Markup("[dim]...[/]");

        var s = build.Stages;
        var name = s.CurrentStageName is not null
            ? $" [dim]{Markup.Escape(s.CurrentStageName)}[/]"
            : "";
        return new Markup($"[yellow]{s.CurrentIndex}/{s.Total}[/]{name}");
    }

    private static Markup FormatResult(BuildResult? result)
    {
        return result switch
        {
            BuildResult.Succeeded => new Markup("[green]Succeeded[/]"),
            BuildResult.Failed => new Markup("[red]Failed[/]"),
            BuildResult.PartiallySucceeded => new Markup("[yellow]Partial[/]"),
            BuildResult.Canceled => new Markup("[grey]Canceled[/]"),
            BuildResult.None or null => new Markup("[dim]-[/]"),
            _ => new Markup(result.ToString()!)
        };
    }

    private static Markup FormatDuration(BuildInfo? build)
    {
        if (build is null) return new Markup("[dim]-[/]");

        var start = build.StartTime ?? build.QueueTime;
        if (start is null) return new Markup("[dim]-[/]");

        var end = build.FinishTime ?? DateTime.UtcNow;
        var duration = end - start.Value;

        var text = duration.TotalHours >= 1
            ? $"{(int)duration.TotalHours}h {duration.Minutes:D2}m"
            : $"{duration.Minutes}m {duration.Seconds:D2}s";

        return new Markup($"[dim]{text}[/]");
    }

    private static Markup FormatLastUpdated(PipelineState state)
    {
        if (state.LastError is not null)
            return new Markup($"[red]{Markup.Escape(state.LastError)}[/]");

        if (state.LastPolledAt is null)
            return new Markup("[dim]Pending...[/]");

        var ago = (int)(DateTime.UtcNow - state.LastPolledAt.Value).TotalSeconds;
        return new Markup($"[dim]{ago}s ago[/]");
    }

    public static string GetColumnHeader(DashboardColumn col) => col switch
    {
        DashboardColumn.Pipeline => "Pipeline",
        DashboardColumn.Organization => "Org",
        DashboardColumn.Project => "Project",
        DashboardColumn.Build => "Build",
        DashboardColumn.Branch => "Branch",
        DashboardColumn.Status => "Status",
        DashboardColumn.Result => "Result",
        DashboardColumn.Stages => "Stages",
        DashboardColumn.Duration => "Duration",
        DashboardColumn.TriggeredBy => "Triggered By",
        DashboardColumn.LastPoll => "Last Poll",
        _ => col.ToString()
    };

    public static bool IsCentered(DashboardColumn col) => col is
        DashboardColumn.Build or
        DashboardColumn.Status or
        DashboardColumn.Stages or
        DashboardColumn.Result;
}
