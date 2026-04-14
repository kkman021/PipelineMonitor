# azmon — Azure DevOps Pipeline Monitor

A terminal dashboard for monitoring multiple Azure DevOps Pipeline build statuses in real time. Optimized polling groups pipelines by organization/project to minimize API requests.

## Interface

```
╭───────────────┬────────┬────────┬─────────┬─────────┬───────────┬──────────┬───────────╮
│ Pipeline      │ Build  │ Branch │ Status  │ Stages  │  Result   │ Duration │ Last Poll │
├───────────────┼────────┼────────┼─────────┼─────────┼───────────┼──────────┼───────────┤
│ Management CI │ #18785 │ dev    │ Running │  2/4    │     -     │ 3m 12s   │ 8s ago    │
│               │        │        │         │ Deploy  │           │          │           │
│ Management CD │ #18761 │ dev    │  Done   │    -    │ Succeeded │ 1m 05s   │ 8s ago    │
│ Frontend CI   │ #9021  │ main   │ Queued  │    -    │     -     │    -     │ 8s ago    │
╰───────────────┴────────┴────────┴─────────┴─────────┴───────────┴──────────┴───────────╯
    Press [Ctrl+R] to refresh now | [Ctrl+C] to exit | Next refresh in: 291s
```

- **Build** column is a clickable hyperlink (opens in browser) in supported terminals
- **Stages** column shows `current/total StageName` when a pipeline is running, and requires YAML-based pipelines (Classic pipelines show `-`)
- Pressing `Ctrl+R` or `F5` forces an immediate refresh without waiting for the polling interval
- `watch` launches in an alternate screen buffer — exiting restores your previous terminal content

## Installation

Download the pre-built binary for your platform from the [Releases](https://github.com/kkman021/PipelineMonitor/releases) page:

| Platform        | File                     |
|-----------------|--------------------------|
| Windows x64     | `azmon-win-x64.exe`      |
| Linux x64       | `azmon-linux-x64`        |
| macOS x64       | `azmon-osx-x64`          |
| macOS ARM64     | `azmon-osx-arm64`        |

**Linux / macOS:** make the binary executable after download:
```bash
chmod +x azmon-linux-x64
```

Optionally move it to a directory on your `PATH` (e.g., `/usr/local/bin/azmon`).

### Build from source

Requires [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).

```bash
git clone https://github.com/kkman021/PipelineMonitor.git
cd PipelineMonitor
dotnet publish src/AzureSummary.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish/
```

Replace `win-x64` with your target platform: `linux-x64`, `osx-x64`, `osx-arm64`.

## Supported Terminals

All terminals support basic functionality. Clickable links in the **Build** column require OSC 8 hyperlink support:

| Terminal           | Clickable Links | Alternate Screen |
|--------------------|:-:|:-:|
| Windows Terminal   | Yes | Yes |
| iTerm2 (macOS)     | Yes | Yes |
| GNOME Terminal     | Yes | Yes |
| Alacritty          | Yes | Yes |
| cmd.exe            | No  | No  |
| PowerShell ISE     | No  | No  |

## Supported Operating Systems

| OS                  | Architecture     |
|---------------------|------------------|
| Windows 10 / 11    | x64              |
| Windows Server 2019+ | x64            |
| Ubuntu 20.04+       | x64              |
| Debian 11+          | x64              |
| macOS 12+           | x64, ARM64       |

## Azure DevOps PAT Setup

### Required permissions

Create a PAT with the minimum required scope:

| Scope   | Permission |
|---------|------------|
| Build   | Read       |

All other scopes can be left unchecked.

### Steps to create a PAT

1. Sign in to your Azure DevOps organization: `https://dev.azure.com/<your-organization>`
2. Click your profile picture (top-right) → **Personal access tokens**
3. Click **New Token**
4. Fill in:
   - **Name**: anything descriptive (e.g., `azmon`)
   - **Organization**: select the target organization
   - **Expiration**: set an appropriate duration
   - **Scopes**: choose **Custom defined**, then check **Build → Read**
5. Click **Create** and copy the token immediately (it is only shown once)

> **Multi-organization:** Azure DevOps PATs are scoped to a single organization. If you monitor pipelines across multiple organizations, create a separate PAT per organization and use `--pat` when adding pipelines from other organizations.

### Configure the PAT

```bash
# Set global PAT (used for all pipelines unless overridden)
azmon config --pat <your-token>

# Set a pipeline-specific PAT (overrides global for that pipeline only)
azmon add <org> <project> <definitionId> --pat <other-token>
```

## Usage

### Quick start

```bash
# 1. Set PAT
azmon config --pat <your-token>

# 2. Add pipelines to monitor
azmon add my-org my-project 42 -n "Backend CI"
azmon add my-org my-project 44 -n "Backend CD"

# 3. Start the dashboard
azmon watch
```

### All commands

#### `azmon config`

Configure global settings. Changes are persisted to `~/.azuremonitor/config.json`.

```bash
azmon config --pat <token>         # Set global PAT
azmon config --interval <seconds>  # Set default polling interval (minimum: 10)
```

#### `azmon add`

Add a pipeline to monitor.

```bash
azmon add <organization> <project> <definitionId> [options]

Options:
  -n, --name <name>   Custom display name shown in the dashboard
  --pat <token>       Pipeline-specific PAT (overrides global PAT for this entry)
```

The `<definitionId>` is the integer ID of the pipeline definition. Find it in the Azure DevOps URL when viewing a pipeline: `.../_build?definitionId=42`.

#### `azmon remove`

Remove a monitored pipeline.

```bash
azmon remove --id <guid>                                        # By local ID (from list)
azmon remove --org <org> --project <project> --definition <id>  # By coordinates
```

#### `azmon list`

List all configured pipelines with their IDs, display names, and PAT source.

```bash
azmon list
```

#### `azmon columns`

Show or configure which columns are visible in the dashboard.

```bash
azmon columns                          # Show current visibility status
azmon columns --show Organization,Project,TriggeredBy
azmon columns --hide TriggeredBy
azmon columns --reset                  # Reset to defaults
```

Available columns: `Pipeline`, `Organization`, `Project`, `Build`, `Branch`, `Status`, `Stages`, `Result`, `Duration`, `TriggeredBy`, `LastPoll`

Default visible: `Pipeline`, `Build`, `Branch`, `Status`, `Stages`, `Result`, `Duration`, `LastPoll`

#### `azmon watch`

Start the live monitoring dashboard.

```bash
azmon watch                 # Use configured polling interval (default: 300s)
azmon watch -i <seconds>    # Override interval for this session only
```

**Keyboard shortcuts while watching:**

| Key        | Action              |
|------------|---------------------|
| `Ctrl+R`   | Force immediate refresh |
| `F5`       | Force immediate refresh |
| `Ctrl+C`   | Exit dashboard      |

### Configuration file

Settings are stored at `~/.azuremonitor/config.json` (Windows: `C:\Users\<user>\.azuremonitor\config.json`).

The file is created automatically on first use. Example:

```json
{
  "globalPat": "***",
  "pollingIntervalSeconds": 300,
  "visibleColumns": ["Pipeline", "Build", "Branch", "Status", "Stages", "Result", "Duration", "LastPoll"],
  "pipelines": [
    {
      "id": "b9fbac87-cfa2-4ded-a65f-98989ca6e950",
      "organization": "my-org",
      "project": "my-project",
      "definitionId": 42,
      "displayName": "Backend CI",
      "overridePat": null,
      "addedAt": "2026-04-14T00:00:00Z"
    }
  ]
}
```

## Polling behavior

- Pipelines sharing the same organization and project are batched into a single API request per poll cycle
- When a pipeline is `Running`, one additional Timeline API call is made to retrieve stage progress
- On HTTP 429 (rate limit), exponential backoff is applied per organization/project group: `5s → 10s → 20s → 40s → 80s → 160s → 300s`
- The display refreshes every second (countdown timer); data is updated only on polling interval or manual refresh
