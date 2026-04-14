using System.ComponentModel;
using Spectre.Console.Cli;

namespace AzureSummary.Commands.Settings;

public class ConfigCommandSettings : CommandSettings
{
    [CommandOption("--pat")]
    [Description("Set the global Personal Access Token")]
    public string? Pat { get; set; }

    [CommandOption("--interval")]
    [Description("Set the default polling interval in seconds (persists to config)")]
    public int? IntervalSeconds { get; set; }
}
