using AzureSummary.Models;
using AzureSummary.Services;
using Spectre.Console;

namespace AzureSummary.Display;

public class LiveTableRenderer(PollingEngine engine, IConfigurationService configService)
{
    private readonly PollingEngine _engine = engine;
    private readonly IConfigurationService _configService = configService;

    public async Task RenderAsync(CancellationToken ct)
    {
        await AnsiConsole.Live(BuildTable())
            .AutoClear(false)
            .Overflow(VerticalOverflow.Ellipsis)
            .StartAsync(async ctx =>
            {
                while (!ct.IsCancellationRequested)
                {
                    try
                    {
                        ctx.UpdateTarget(BuildTable());
                        await Task.Delay(TimeSpan.FromSeconds(1), ct);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            });
    }

    private Spectre.Console.Table BuildTable()
    {
        var config = _configService.Load();
        var visibleColumns = config.VisibleColumns.Count > 0
            ? config.VisibleColumns
            : [DashboardColumn.Pipeline, DashboardColumn.Status, DashboardColumn.Result];

        var table = new Spectre.Console.Table().Border(TableBorder.Rounded);

        foreach (var col in visibleColumns)
        {
            var header = StatusFormatter.GetColumnHeader(col);
            var tc = new TableColumn(header);
            if (StatusFormatter.IsCentered(col))
                tc = tc.Centered();
            table.AddColumn(tc);
        }

        foreach (var state in _engine.CurrentStates.Values)
        {
            table.AddRow(StatusFormatter.FormatRow(state, visibleColumns));
        }

        if (_engine.CurrentStates.Count == 0)
        {
            var empty = new string[visibleColumns.Count];
            empty[0] = "[dim]No pipelines configured. Run: azmon add <org> <project> <definitionId>[/]";
            for (var i = 1; i < empty.Length; i++) empty[i] = "";
            table.AddRow(empty);
        }

        string caption;
        if (_engine.IsQuietHoursActive)
        {
            var qh = config.QuietHours;
            caption = $"[dim]Quiet hours active ({qh.Start:HH\\:mm}–{qh.End:HH\\:mm}) | Press [[Ctrl+R]] to refresh once | [[Ctrl+C]] to exit[/]";
        }
        else
        {
            var nextRefresh = _engine.NextPollAt;
            var countdown = nextRefresh.HasValue
                ? $"{Math.Max(0, (int)(nextRefresh.Value - DateTime.UtcNow).TotalSeconds)}s"
                : "...";
            caption = $"[dim]Press [[Ctrl+R]] to refresh now | [[Ctrl+C]] to exit | Next refresh in: {countdown}[/]";
        }

        table.Caption(caption);

        return table;
    }
}
