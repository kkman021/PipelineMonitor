using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using AzureSummary.Infrastructure;
using AzureSummary.Models;

namespace AzureSummary.Services;

public interface IAzureDevOpsService
{
    Task<IReadOnlyList<BuildInfo>> GetLatestBuildsAsync(
        string organization,
        string project,
        IEnumerable<int> definitionIds,
        string pat,
        CancellationToken ct = default);

    Task<StageProgress?> GetStageProgressAsync(
        string organization,
        string project,
        int buildId,
        string pat,
        CancellationToken ct = default);

    Task<bool> ValidatePatAsync(string organization, string pat, CancellationToken ct = default);
}

public class AzureDevOpsService : IAzureDevOpsService
{
    private readonly AzureDevOpsHttpClientFactory _httpClientFactory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public AzureDevOpsService(AzureDevOpsHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<IReadOnlyList<BuildInfo>> GetLatestBuildsAsync(
        string organization,
        string project,
        IEnumerable<int> definitionIds,
        string pat,
        CancellationToken ct = default)
    {
        var ids = definitionIds.ToList();
        if (ids.Count == 0) return [];

        var client = _httpClientFactory.GetOrCreate(pat);
        var definitionsParam = string.Join(",", ids);
        var top = ids.Count * 2;

        var url = $"https://dev.azure.com/{Uri.EscapeDataString(organization)}/{Uri.EscapeDataString(project)}" +
                  $"/_apis/build/builds?definitions={definitionsParam}&$top={top}" +
                  $"&queryOrder=queueTimeDescending&api-version=7.1";

        var response = await client.GetAsync(url, ct);

        if (response.StatusCode == HttpStatusCode.TooManyRequests)
            throw new HttpRequestException("Rate limit exceeded", null, HttpStatusCode.TooManyRequests);

        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(ct);
        var apiResponse = JsonSerializer.Deserialize<AzureBuildsResponse>(content, JsonOptions);

        if (apiResponse?.Value is null) return [];

        return apiResponse.Value
            .GroupBy(b => b.Definition?.Id ?? 0)
            .Select(g => g.OrderByDescending(b => b.Id).First())
            .Select(MapToBuildInfo)
            .ToList();
    }

    public async Task<StageProgress?> GetStageProgressAsync(
        string organization,
        string project,
        int buildId,
        string pat,
        CancellationToken ct = default)
    {
        try
        {
            var client = _httpClientFactory.GetOrCreate(pat);
            var url = $"https://dev.azure.com/{Uri.EscapeDataString(organization)}/{Uri.EscapeDataString(project)}" +
                      $"/_apis/build/builds/{buildId}/timeline?api-version=7.1";

            var response = await client.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode) return null;

            var content = await response.Content.ReadAsStringAsync(ct);
            var timeline = JsonSerializer.Deserialize<AzureTimelineResponse>(content, JsonOptions);

            if (timeline?.Records is null) return null;

            // Filter stage-level records only, sorted by order
            var stages = timeline.Records
                .Where(r => string.Equals(r.Type, "Stage", StringComparison.OrdinalIgnoreCase))
                .OrderBy(r => r.Order)
                .ToList();

            if (stages.Count == 0) return null;

            // Find the in-progress stage (lowest order among inProgress)
            var currentStage = stages
                .Where(s => string.Equals(s.State, "inProgress", StringComparison.OrdinalIgnoreCase))
                .MinBy(s => s.Order);

            if (currentStage is null) return null;

            var currentIndex = stages.IndexOf(currentStage) + 1; // 1-based

            return new StageProgress
            {
                CurrentIndex = currentIndex,
                Total = stages.Count,
                CurrentStageName = currentStage.Name
            };
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> ValidatePatAsync(string organization, string pat, CancellationToken ct = default)
    {
        try
        {
            var client = _httpClientFactory.GetOrCreate(pat);
            var url = $"https://dev.azure.com/{Uri.EscapeDataString(organization)}/_apis/connectionData?api-version=7.1";
            var response = await client.GetAsync(url, ct);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private static BuildInfo MapToBuildInfo(AzureBuildDto dto)
    {
        return new BuildInfo
        {
            BuildId = dto.Id,
            DefinitionId = dto.Definition?.Id ?? 0,
            DefinitionName = dto.Definition?.Name ?? "",
            Status = ParseStatus(dto.Status),
            Result = ParseResult(dto.Result),
            RequestedBy = dto.RequestedBy?.DisplayName,
            QueueTime = dto.QueueTime,
            StartTime = dto.StartTime,
            FinishTime = dto.FinishTime,
            SourceBranch = dto.SourceBranch,
            WebUrl = dto.Links?.Web?.Href
        };
    }

    private static BuildStatus ParseStatus(string? status) => status?.ToLowerInvariant() switch
    {
        "inprogress" => BuildStatus.InProgress,
        "completed" => BuildStatus.Completed,
        "cancelling" => BuildStatus.Cancelling,
        "postponed" => BuildStatus.Postponed,
        "notstarted" => BuildStatus.NotStarted,
        "none" => BuildStatus.None,
        _ => BuildStatus.Unknown
    };

    private static BuildResult ParseResult(string? result) => result?.ToLowerInvariant() switch
    {
        "succeeded" => BuildResult.Succeeded,
        "partiallysucceeded" => BuildResult.PartiallySucceeded,
        "failed" => BuildResult.Failed,
        "canceled" => BuildResult.Canceled,
        _ => BuildResult.None
    };

    // Internal DTOs for deserialization only
    private class AzureBuildsResponse
    {
        [JsonPropertyName("value")]
        public List<AzureBuildDto> Value { get; set; } = [];
    }

    private class AzureBuildDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("result")]
        public string? Result { get; set; }

        [JsonPropertyName("queueTime")]
        public DateTime? QueueTime { get; set; }

        [JsonPropertyName("startTime")]
        public DateTime? StartTime { get; set; }

        [JsonPropertyName("finishTime")]
        public DateTime? FinishTime { get; set; }

        [JsonPropertyName("sourceBranch")]
        public string? SourceBranch { get; set; }

        [JsonPropertyName("requestedBy")]
        public AzureIdentityDto? RequestedBy { get; set; }

        [JsonPropertyName("definition")]
        public AzureDefinitionDto? Definition { get; set; }

        [JsonPropertyName("_links")]
        public AzureLinksDto? Links { get; set; }
    }

    private class AzureIdentityDto
    {
        [JsonPropertyName("displayName")]
        public string? DisplayName { get; set; }
    }

    private class AzureDefinitionDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }

    private class AzureLinksDto
    {
        [JsonPropertyName("web")]
        public AzureLinkDto? Web { get; set; }
    }

    private class AzureLinkDto
    {
        [JsonPropertyName("href")]
        public string? Href { get; set; }
    }

    private class AzureTimelineResponse
    {
        [JsonPropertyName("records")]
        public List<AzureTimelineRecord> Records { get; set; } = [];
    }

    private class AzureTimelineRecord
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("order")]
        public int Order { get; set; }

        [JsonPropertyName("state")]
        public string? State { get; set; }

        [JsonPropertyName("result")]
        public string? Result { get; set; }
    }
}
