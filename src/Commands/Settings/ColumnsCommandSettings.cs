using System.ComponentModel;
using Spectre.Console.Cli;

namespace AzureSummary.Commands.Settings;

public class ColumnsCommandSettings : CommandSettings
{
    [CommandOption("--show")]
    [Description("Comma-separated columns to show (e.g. Organization,Project,TriggeredBy)")]
    public string? Show { get; set; }

    [CommandOption("--hide")]
    [Description("Comma-separated columns to hide")]
    public string? Hide { get; set; }

    [CommandOption("--reset")]
    [Description("Reset to default column visibility")]
    public bool Reset { get; set; }
}
