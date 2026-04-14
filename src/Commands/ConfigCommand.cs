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
        var hasAnyOption = settings.Pat is not null
            || settings.IntervalSeconds is not null
            || settings.Quiet is not null
            || settings.QuietStart is not null
            || settings.QuietEnd is not null
            || settings.QuietZone is not null;

        if (!hasAnyOption)
        {
            AnsiConsole.MarkupLine("[red]Error:[/] Specify at least one option.");
            AnsiConsole.MarkupLine("[dim]Options: --pat, --interval, --quiet on|off, --quiet-start HH:mm, --quiet-end HH:mm, --quiet-zone <tz>[/]");
            return 1;
        }

        var config = _configService.Load();

        if (settings.Pat is not null)
        {
            config.GlobalPat = settings.Pat;
            AnsiConsole.MarkupLine("[green]Global PAT updated.[/]");
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
        }

        if (settings.Quiet is not null)
        {
            var enable = settings.Quiet.Equals("on", StringComparison.OrdinalIgnoreCase);
            var disable = settings.Quiet.Equals("off", StringComparison.OrdinalIgnoreCase);
            if (!enable && !disable)
            {
                AnsiConsole.MarkupLine("[red]Error:[/] --quiet must be 'on' or 'off'.");
                return 1;
            }
            config.QuietHours.Enabled = enable;
            AnsiConsole.MarkupLine($"[green]Quiet hours {(enable ? "enabled" : "disabled")}.[/]");
        }

        if (settings.QuietStart is not null)
        {
            if (!TimeOnly.TryParseExact(settings.QuietStart, "HH:mm", out var t))
            {
                AnsiConsole.MarkupLine("[red]Error:[/] --quiet-start must be in HH:mm format (e.g. 18:00).");
                return 1;
            }
            config.QuietHours.Start = t;
            AnsiConsole.MarkupLine($"[green]Quiet hours start set to {t:HH:mm}.[/]");
        }

        if (settings.QuietEnd is not null)
        {
            if (!TimeOnly.TryParseExact(settings.QuietEnd, "HH:mm", out var t))
            {
                AnsiConsole.MarkupLine("[red]Error:[/] --quiet-end must be in HH:mm format (e.g. 08:00).");
                return 1;
            }
            config.QuietHours.End = t;
            AnsiConsole.MarkupLine($"[green]Quiet hours end set to {t:HH:mm}.[/]");
        }

        if (settings.QuietZone is not null)
        {
            try
            {
                TimeZoneInfo.FindSystemTimeZoneById(settings.QuietZone);
                config.QuietHours.TimeZoneId = settings.QuietZone;
                AnsiConsole.MarkupLine($"[green]Quiet hours timezone set to {settings.QuietZone}.[/]");
            }
            catch
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] Unknown timezone '{settings.QuietZone}'.");
                AnsiConsole.MarkupLine("[dim]Use IANA IDs (e.g. Asia/Taipei, America/New_York) or Windows IDs (e.g. Taipei Standard Time).[/]");
                return 1;
            }
        }

        _configService.Save(config);
        return 0;
    }
}
