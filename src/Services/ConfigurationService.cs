using System.Text.Json;
using System.Text.Json.Serialization;
using AzureSummary.Models;

namespace AzureSummary.Services;

public interface IConfigurationService
{
    AppConfig Load();
    void Save(AppConfig config);
    void AddPipeline(PipelineEntry entry);
    bool RemovePipelineById(Guid id);
    bool RemovePipelineByCoordinates(string org, string project, int definitionId);
    string GetConfigFilePath();
}

public class ConfigurationService : IConfigurationService
{
    private static readonly string ConfigDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".azuremonitor");

    private static readonly string ConfigPath = Path.Combine(ConfigDir, "config.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    public AppConfig Load()
    {
        if (!File.Exists(ConfigPath))
            return new AppConfig();

        var json = File.ReadAllText(ConfigPath);
        return JsonSerializer.Deserialize<AppConfig>(json, JsonOptions) ?? new AppConfig();
    }

    public void Save(AppConfig config)
    {
        Directory.CreateDirectory(ConfigDir);
        var json = JsonSerializer.Serialize(config, JsonOptions);
        File.WriteAllText(ConfigPath, json);

        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(ConfigPath,
                UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
    }

    public void AddPipeline(PipelineEntry entry)
    {
        var config = Load();

        var exists = config.Pipelines.Any(p =>
            string.Equals(p.Organization, entry.Organization, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(p.Project, entry.Project, StringComparison.OrdinalIgnoreCase) &&
            p.DefinitionId == entry.DefinitionId);

        if (exists)
            throw new InvalidOperationException(
                $"Pipeline {entry.Organization}/{entry.Project}/{entry.DefinitionId} already exists.");

        config.Pipelines.Add(entry);
        Save(config);
    }

    public bool RemovePipelineById(Guid id)
    {
        var config = Load();
        var target = config.Pipelines.FirstOrDefault(p => p.Id == id);
        if (target is null) return false;
        config.Pipelines.Remove(target);
        Save(config);
        return true;
    }

    public bool RemovePipelineByCoordinates(string org, string project, int definitionId)
    {
        var config = Load();
        var target = config.Pipelines.FirstOrDefault(p =>
            string.Equals(p.Organization, org, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(p.Project, project, StringComparison.OrdinalIgnoreCase) &&
            p.DefinitionId == definitionId);

        if (target is null) return false;
        config.Pipelines.Remove(target);
        Save(config);
        return true;
    }

    public string GetConfigFilePath() => ConfigPath;
}
