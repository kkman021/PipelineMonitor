using AzureSummary.Commands.Settings;
using AzureSummary.Services;
using Spectre.Console;
using Spectre.Console.Cli;

namespace AzureSummary.Commands;

public class ConfigCommand(IConfigurationService configService) : Command<ConfigCommandSettings>
{
    private readonly IConfigurationService _configService = configService;

    public override int Execute(CommandContext context, ConfigCommandSettings settings)
    {
        if (settings.Pat is null && settings.IntervalSeconds is null)
        {
            AnsiConsole.MarkupLine("[red]Error:[/] Specify --pat and/or --interval.");
            return 1;
        }

        var config = _configService.Load();
        var changed = false;

        if (settings.Pat is not null)
        {
            config.GlobalPat = settings.Pat;
            AnsiConsole.MarkupLine("[green]Global PAT updated.[/]");
            changed = true;
        }

        if (settings.IntervalSeconds.HasValue)
        {
            if (settings.IntervalSeconds.Value < 10)
            {
                AnsiConsole.MarkupLine("[red]Error:[/] Interval must be at least 10 seconds.");
                return 1;
            }
            config.PollingIntervalSeconds = settings.IntervalSeconds.Value;
            AnsiConsole.MarkupLine($"[green]Polling interval set to {settings.IntervalSeconds.Value}s.[/]");
            changed = true;
        }

        if (changed)
            _configService.Save(config);

        return 0;
    }
}
