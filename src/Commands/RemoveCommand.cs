using AzureSummary.Commands.Settings;
using AzureSummary.Services;
using Spectre.Console;
using Spectre.Console.Cli;

namespace AzureSummary.Commands;

public class RemoveCommand(IConfigurationService configService) : Command<RemoveCommandSettings>
{
    private readonly IConfigurationService _configService = configService;

    public override int Execute(CommandContext context, RemoveCommandSettings settings)
    {
        if (settings.LocalId.HasValue)
        {
            var removed = _configService.RemovePipelineById(settings.LocalId.Value);
            if (!removed)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] No pipeline found with ID {settings.LocalId}");
                return 1;
            }
            AnsiConsole.MarkupLine("[green]Removed.[/]");
            return 0;
        }

        if (settings.Organization is not null && settings.Project is not null && settings.DefinitionId.HasValue)
        {
            var removed = _configService.RemovePipelineByCoordinates(
                settings.Organization, settings.Project, settings.DefinitionId.Value);

            if (!removed)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] No pipeline found for {settings.Organization}/{settings.Project}/{settings.DefinitionId}");
                return 1;
            }
            AnsiConsole.MarkupLine("[green]Removed.[/]");
            return 0;
        }

        AnsiConsole.MarkupLine("[red]Error:[/] Specify either --id <guid> or --org/--project/--definition.");
        return 1;
    }
}
