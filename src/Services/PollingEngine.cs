using System.Collections.Concurrent;
using System.Net;
using AzureSummary.Models;

namespace AzureSummary.Services;

public class StatusChangedEventArgs(PipelineState state) : EventArgs
{
    public PipelineState State { get; } = state;
}

public class PollingEngine
{
    private readonly IAzureDevOpsService _devOpsService;
    private readonly IConfigurationService _configService;

    private readonly ConcurrentDictionary<Guid, PipelineState> _states = new();
    private readonly ConcurrentDictionary<string, BackoffState> _backoffStates = new();

    private TaskCompletionSource _forceRefreshTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _intervalSeconds;

    public event EventHandler<StatusChangedEventArgs>? StatusChanged;

    public IReadOnlyDictionary<Guid, PipelineState> CurrentStates => _states;

    public DateTime? NextPollAt { get; private set; }
    public bool IsQuietHoursActive { get; private set; }

    public PollingEngine(IAzureDevOpsService devOpsService, IConfigurationService configService)
    {
        _devOpsService = devOpsService;
        _configService = configService;
    }

    public void TriggerRefresh()
    {
        _forceRefreshTcs.TrySetResult();
    }

    public async Task RunAsync(int intervalSeconds, CancellationToken ct)
    {
        _intervalSeconds = intervalSeconds;
        InitializeStates();

        while (!ct.IsCancellationRequested)
        {
            _forceRefreshTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            var config = _configService.Load();
            IsQuietHoursActive = config.QuietHours.IsActive();

            if (!IsQuietHoursActive)
            {
                await PollAllGroupsAsync(ct);
                NextPollAt = DateTime.UtcNow.AddSeconds(_intervalSeconds);
            }
            else
            {
                NextPollAt = null;
            }

            // During quiet hours: wake up every minute to re-check, or on force refresh
            var delay = IsQuietHoursActive
                ? TimeSpan.FromMinutes(1)
                : TimeSpan.FromSeconds(_intervalSeconds);

            await Task.WhenAny(Task.Delay(delay, ct), _forceRefreshTcs.Task);

            // Force refresh during quiet hours polls once immediately
            if (_forceRefreshTcs.Task.IsCompleted && IsQuietHoursActive)
            {
                await PollAllGroupsAsync(ct);
            }
        }
    }

    private void InitializeStates()
    {
        _states.Clear();
        var config = _configService.Load();
        foreach (var entry in config.Pipelines)
        {
            _states[entry.Id] = new PipelineState { Entry = entry };
        }
    }

    public void ReloadPipelines()
    {
        var config = _configService.Load();
        var currentIds = _states.Keys.ToHashSet();
        var newIds = config.Pipelines.Select(p => p.Id).ToHashSet();

        // Remove pipelines no longer in config
        foreach (var id in currentIds.Except(newIds))
            _states.TryRemove(id, out _);

        // Add new pipelines
        foreach (var entry in config.Pipelines.Where(p => !currentIds.Contains(p.Id)))
            _states[entry.Id] = new PipelineState { Entry = entry };
    }

    private async Task PollAllGroupsAsync(CancellationToken ct)
    {
        var config = _configService.Load();
        var globalPat = config.GlobalPat ?? "";

        var groups = _states.Values
            .GroupBy(s => (
                s.Entry.Organization.ToLowerInvariant(),
                s.Entry.Project.ToLowerInvariant()))
            .Select(g => new PipelineGroup
            {
                Organization = g.First().Entry.Organization,
                Project = g.First().Entry.Project,
                Entries = g.Select(s => s.Entry).ToList(),
                Pat = g.First().Entry.OverridePat ?? globalPat
            })
            .ToList();

        var tasks = groups.Select(group => PollGroupAsync(group, ct));
        await Task.WhenAll(tasks);
    }

    private async Task PollGroupAsync(PipelineGroup group, CancellationToken ct)
    {
        var groupKey = $"{group.Organization.ToLowerInvariant()}/{group.Project.ToLowerInvariant()}";

        if (_backoffStates.TryGetValue(groupKey, out var backoff) && backoff.IsActive)
            return;

        if (string.IsNullOrWhiteSpace(group.Pat))
        {
            foreach (var entry in group.Entries)
            {
                if (_states.TryGetValue(entry.Id, out var s))
                    s.LastError = "No PAT configured. Run: azmon config --pat <token>";
            }
            return;
        }

        try
        {
            var definitionIds = group.Entries.Select(e => e.DefinitionId);
            var builds = await _devOpsService.GetLatestBuildsAsync(
                group.Organization, group.Project, definitionIds, group.Pat, ct);

            var buildMap = builds.ToDictionary(b => b.DefinitionId);

            foreach (var entry in group.Entries)
            {
                if (!_states.TryGetValue(entry.Id, out var state)) continue;

                buildMap.TryGetValue(entry.DefinitionId, out var newBuild);
                UpdateState(state, newBuild);
            }

            // Fetch stage progress in parallel for all in-progress builds
            var timelineTasks = builds
                .Where(b => b.Status == BuildStatus.InProgress)
                .Select(async b =>
                {
                    var stages = await _devOpsService.GetStageProgressAsync(
                        group.Organization, group.Project, b.BuildId, group.Pat, ct);
                    b.Stages = stages;
                });
            await Task.WhenAll(timelineTasks);

            _backoffStates.TryRemove(groupKey, out _);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.TooManyRequests)
        {
            var bs = _backoffStates.GetOrAdd(groupKey, _ => new BackoffState());
            bs.Increment();

            foreach (var entry in group.Entries)
            {
                if (_states.TryGetValue(entry.Id, out var s))
                    s.LastError = $"Rate limited. Retrying in {bs.DelaySeconds}s";
            }
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            foreach (var entry in group.Entries)
            {
                if (_states.TryGetValue(entry.Id, out var s))
                    s.LastError = ex.Message;
            }
        }
    }

    private void UpdateState(PipelineState state, BuildInfo? newBuild)
    {
        state.PreviousBuild = state.CurrentBuild;
        state.CurrentBuild = newBuild;
        state.LastPolledAt = DateTime.UtcNow;
        state.LastError = null;

        if (state.HasStatusChanged)
            StatusChanged?.Invoke(this, new StatusChangedEventArgs(state));
    }

    private record PipelineGroup
    {
        public string Organization { get; init; } = "";
        public string Project { get; init; } = "";
        public List<PipelineEntry> Entries { get; init; } = [];
        public string Pat { get; init; } = "";
    }

    private class BackoffState
    {
        private static readonly int[] Delays = [5, 10, 20, 40, 80, 160, 300];
        private int _level;

        public int DelaySeconds => Delays[Math.Min(_level - 1, Delays.Length - 1)];
        public DateTime RetryAfter { get; private set; } = DateTime.MinValue;
        public bool IsActive => DateTime.UtcNow < RetryAfter;

        public void Increment()
        {
            var delay = Delays[Math.Min(_level, Delays.Length - 1)];
            RetryAfter = DateTime.UtcNow.AddSeconds(delay);
            _level = Math.Min(_level + 1, Delays.Length - 1);
        }
    }
}
