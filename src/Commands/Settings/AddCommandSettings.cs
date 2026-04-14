using System.ComponentModel;
using Spectre.Console.Cli;

namespace AzureSummary.Commands.Settings;

public class AddCommandSettings : CommandSettings
{
    [CommandArgument(0, "<organization>")]
    [Description("Azure DevOps organization name")]
    public string Organization { get; set; } = "";

    [CommandArgument(1, "<project>")]
    [Description("Project name")]
    public string Project { get; set; } = "";

    [CommandArgument(2, "<definitionId>")]
    [Description("Pipeline definition ID (integer)")]
    public int DefinitionId { get; set; }

    [CommandOption("-n|--name")]
    [Description("Custom display name")]
    public string? DisplayName { get; set; }

    [CommandOption("--pat")]
    [Description("PAT override for this pipeline (uses global PAT if omitted)")]
    public string? Pat { get; set; }
}
