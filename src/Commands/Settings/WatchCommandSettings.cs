using System.ComponentModel;
using Spectre.Console.Cli;

namespace AzureSummary.Commands.Settings;

public class WatchCommandSettings : CommandSettings
{
    [CommandOption("-i|--interval")]
    [Description("Polling interval in seconds for this session (does not persist; use 'config --interval' to persist)")]
    public int? IntervalSeconds { get; set; }
}
