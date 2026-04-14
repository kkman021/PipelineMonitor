using System.ComponentModel;
using Spectre.Console.Cli;

namespace AzureSummary.Commands.Settings;

public class RemoveCommandSettings : CommandSettings
{
    [CommandOption("--id")]
    [Description("Local pipeline GUID (from list command)")]
    public Guid? LocalId { get; set; }

    [CommandOption("--org")]
    [Description("Organization name")]
    public string? Organization { get; set; }

    [CommandOption("--project")]
    [Description("Project name")]
    public string? Project { get; set; }

    [CommandOption("--definition")]
    [Description("Pipeline definition ID")]
    public int? DefinitionId { get; set; }
}
