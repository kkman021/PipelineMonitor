using AzureSummary.Commands.Settings;
using AzureSummary.Models;
using AzureSummary.Services;
using Spectre.Console;
using Spectre.Console.Cli;

namespace AzureSummary.Commands;

public class ColumnsCommand(IConfigurationService configService) : Command<ColumnsCommandSettings>
{
    private static readonly List<DashboardColumn> DefaultColumns =
    [
        DashboardColumn.Pipeline,
        DashboardColumn.Build,
        DashboardColumn.Branch,
        DashboardColumn.Status,
        DashboardColumn.Result,
        DashboardColumn.Duration,
        DashboardColumn.LastPoll
    ];

    private readonly IConfigurationService _configService = configService;

    public override int Execute(CommandContext context, ColumnsCommandSettings settings)
    {
        var config = _configService.Load();

        if (settings.Reset)
        {
            config.VisibleColumns = [.. DefaultColumns];
            _configService.Save(config);
            AnsiConsole.MarkupLine("[green]Column visibility reset to defaults.[/]");
            PrintStatus(config.VisibleColumns);
            return 0;
        }

        if (settings.Show is null && settings.Hide is null)
        {
            PrintStatus(config.VisibleColumns);
            return 0;
        }

        if (settings.Show is not null)
        {
            foreach (var col in ParseColumns(settings.Show))
            {
                if (!config.VisibleColumns.Contains(col))
                    config.VisibleColumns.Add(col);
            }
        }

        if (settings.Hide is not null)
        {
            foreach (var col in ParseColumns(settings.Hide))
            {
                if (col == DashboardColumn.Pipeline)
                {
                    AnsiConsole.MarkupLine("[yellow]Warning:[/] Pipeline column cannot be hidden.");
                    continue;
                }
                config.VisibleColumns.Remove(col);
            }
        }

        // Preserve canonical order
        config.VisibleColumns = Enum.GetValues<DashboardColumn>()
            .Where(c => config.VisibleColumns.Contains(c))
            .ToList();

        _configService.Save(config);
        PrintStatus(config.VisibleColumns);
        return 0;
    }

    private static IEnumerable<DashboardColumn> ParseColumns(string input)
    {
        foreach (var part in input.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (Enum.TryParse<DashboardColumn>(part, ignoreCase: true, out var col))
                yield return col;
            else
                AnsiConsole.MarkupLine($"[yellow]Unknown column:[/] {part}");
        }
    }

    private static void PrintStatus(List<DashboardColumn> visibleColumns)
    {
        AnsiConsole.WriteLine();
        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("Column")
            .AddColumn(new TableColumn("Visible").Centered());

        foreach (var col in Enum.GetValues<DashboardColumn>())
        {
            var visible = visibleColumns.Contains(col);
            table.AddRow(
                col.ToString(),
                visible ? "[green]yes[/]" : "[dim]no[/]"
            );
        }

        AnsiConsole.Write(table);
    }
}
