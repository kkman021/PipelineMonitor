using AzureSummary.Commands.Settings;
using AzureSummary.Models;
using AzureSummary.Services;
using Spectre.Console;
using Spectre.Console.Cli;

namespace AzureSummary.Commands;

public class AddCommand(IConfigurationService configService) : Command<AddCommandSettings>
{
    private readonly IConfigurationService _configService = configService;

    public override int Execute(CommandContext context, AddCommandSettings settings)
    {
        var config = _configService.Load();
        var pat = settings.Pat ?? config.GlobalPat;

        if (string.IsNullOrWhiteSpace(pat))
        {
            AnsiConsole.MarkupLine("[yellow]Warning:[/] No PAT specified and no global PAT configured.");
            AnsiConsole.MarkupLine("[dim]Run: azmon config --pat <token>[/]");
        }

        var entry = new PipelineEntry
        {
            Organization = settings.Organization,
            Project = settings.Project,
            DefinitionId = settings.DefinitionId,
            DisplayName = settings.DisplayName,
            OverridePat = settings.Pat
        };

        try
        {
            _configService.AddPipeline(entry);
            AnsiConsole.MarkupLine($"[green]Added:[/] {settings.Organization}/{settings.Project}/{settings.DefinitionId}");
        }
        catch (InvalidOperationException ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            return 1;
        }

        return 0;
    }
}
