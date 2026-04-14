using AzureSummary.Commands.Settings;
using AzureSummary.Services;
using Spectre.Console;
using Spectre.Console.Cli;

namespace AzureSummary.Commands;

public class ListCommand(IConfigurationService configService) : Command<ListCommandSettings>
{
    private readonly IConfigurationService _configService = configService;

    public override int Execute(CommandContext context, ListCommandSettings settings)
    {
        var config = _configService.Load();

        AnsiConsole.MarkupLine($"[dim]Config: {_configService.GetConfigFilePath()}[/]");
        AnsiConsole.MarkupLine($"[dim]Global PAT: {(string.IsNullOrWhiteSpace(config.GlobalPat) ? "not set" : "***")}[/]");
        AnsiConsole.MarkupLine($"[dim]Polling interval: {config.PollingIntervalSeconds}s[/]");
        AnsiConsole.WriteLine();

        if (config.Pipelines.Count == 0)
        {
            AnsiConsole.MarkupLine("[dim]No pipelines configured.[/]");
            AnsiConsole.MarkupLine("[dim]Run: azmon add <organization> <project> <definitionId>[/]");
            return 0;
        }

        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("ID")
            .AddColumn("Organization")
            .AddColumn("Project")
            .AddColumn("Def ID")
            .AddColumn("Display Name")
            .AddColumn("PAT Source");

        foreach (var p in config.Pipelines)
        {
            var patSource = p.OverridePat is not null ? "pipeline-specific" : "global";
            table.AddRow(
                $"[dim]{p.Id}[/]",
                Markup.Escape(p.Organization),
                Markup.Escape(p.Project),
                p.DefinitionId.ToString(),
                p.DisplayName is not null ? Markup.Escape(p.DisplayName) : "[dim]-[/]",
                patSource
            );
        }

        AnsiConsole.Write(table);
        return 0;
    }
}
