using AzureSummary.Commands.Settings;
using AzureSummary.Display;
using AzureSummary.Services;
using Spectre.Console;
using Spectre.Console.Cli;

namespace AzureSummary.Commands;

public class WatchCommand(
    IConfigurationService configService,
    PollingEngine pollingEngine,
    LiveTableRenderer renderer) : AsyncCommand<WatchCommandSettings>
{
    private readonly IConfigurationService _configService = configService;
    private readonly PollingEngine _pollingEngine = pollingEngine;
    private readonly LiveTableRenderer _renderer = renderer;

    public override async Task<int> ExecuteAsync(CommandContext context, WatchCommandSettings settings)
    {
        var config = _configService.Load();

        if (config.Pipelines.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No pipelines configured.[/]");
            AnsiConsole.MarkupLine("[dim]Run: azmon add <organization> <project> <definitionId>[/]");
            return 0;
        }

        if (string.IsNullOrWhiteSpace(config.GlobalPat) &&
            config.Pipelines.All(p => string.IsNullOrWhiteSpace(p.OverridePat)))
        {
            AnsiConsole.MarkupLine("[yellow]Warning:[/] No PAT configured. Polling will fail.");
            AnsiConsole.MarkupLine("[dim]Run: azmon config --pat <token>[/]");
        }

        var intervalSeconds = settings.IntervalSeconds ?? config.PollingIntervalSeconds;

        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        EnterAlternateScreen();
        try
        {
            // Key listener for force refresh
            var keyListenerTask = Task.Run(() => ListenForKeyPress(cts.Token), cts.Token);

            var pollingTask = _pollingEngine.RunAsync(intervalSeconds, cts.Token);
            var displayTask = _renderer.RenderAsync(cts.Token);

            await Task.WhenAny(pollingTask, displayTask);

            await cts.CancelAsync();

            try { await Task.WhenAll(pollingTask, displayTask, keyListenerTask); }
            catch (OperationCanceledException) { }
        }
        finally
        {
            ExitAlternateScreen();
        }

        return 0;
    }

    private static void EnterAlternateScreen()
    {
        Console.Write("\x1b[?1049h"); // switch to alternate screen buffer
        Console.Write("\x1b[H");      // move cursor to top-left
        Console.Write("\x1b[2J");     // clear screen
    }

    private static void ExitAlternateScreen()
    {
        Console.Write("\x1b[?1049l"); // restore original screen buffer
    }

    private void ListenForKeyPress(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                if (!Console.KeyAvailable)
                {
                    Thread.Sleep(100);
                    continue;
                }

                var key = Console.ReadKey(intercept: true);
                var isCtrlR = key.Key == ConsoleKey.R && key.Modifiers.HasFlag(ConsoleModifiers.Control);
                if (isCtrlR || key.Key == ConsoleKey.F5)
                {
                    _pollingEngine.TriggerRefresh();
                }
            }
            catch (InvalidOperationException)
            {
                // Console.KeyAvailable throws when stdin is redirected
                break;
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
