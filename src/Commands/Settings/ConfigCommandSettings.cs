using System.ComponentModel;
using Spectre.Console.Cli;

namespace AzureSummary.Commands.Settings;

public class ConfigCommandSettings : CommandSettings
{
    [CommandOption("--pat")]
    [Description("Set the global Personal Access Token")]
    public string? Pat { get; set; }

    [CommandOption("--interval")]
    [Description("Set the default polling interval in seconds (minimum: 10)")]
    public int? IntervalSeconds { get; set; }

    [CommandOption("--quiet")]
    [Description("Enable or disable quiet hours: on | off")]
    public string? Quiet { get; set; }

    [CommandOption("--quiet-start")]
    [Description("Quiet hours start time in HH:mm (24h, local timezone)")]
    public string? QuietStart { get; set; }

    [CommandOption("--quiet-end")]
    [Description("Quiet hours end time in HH:mm (24h, local timezone)")]
    public string? QuietEnd { get; set; }

    [CommandOption("--quiet-zone")]
    [Description("IANA timezone ID for quiet hours (default: Asia/Taipei)")]
    public string? QuietZone { get; set; }
}
