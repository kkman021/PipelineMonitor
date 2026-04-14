# azmon — Azure DevOps Pipeline Monitor

A terminal dashboard for monitoring multiple Azure DevOps Pipeline build statuses in real time.

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

## Quick Start

**1. Download** the binary for your platform from the [Releases](https://github.com/kkman021/PipelineMonitor/releases) page and rename it to `azmon` (see [Installation](#installation)).

**2. Get a PAT** — in Azure DevOps, go to profile → **Personal access tokens** → New Token → Scopes: **Build → Read** only. ([Detailed steps](#azure-devops-pat-setup))

**3. Configure and run:**

```bash
azmon config --pat <your-token>

azmon add <organization> <project> <definitionId> -n "My Pipeline"

azmon watch
```

---

## Installation

Download from the [Releases](https://github.com/kkman021/PipelineMonitor/releases) page:

| Platform      | File                |
|---------------|---------------------|
| Windows x64   | `azmon-win-x64.exe` |
| Linux x64     | `azmon-linux-x64`   |
| macOS x64     | `azmon-osx-x64`     |
| macOS ARM64   | `azmon-osx-arm64`   |

**Windows** — rename and add to PATH:

```powershell
Rename-Item azmon-win-x64.exe azmon.exe
Move-Item azmon.exe C:\tools\azmon.exe

# Add C:\tools to user PATH (one-time)
[Environment]::SetEnvironmentVariable("PATH", $env:PATH + ";C:\tools", [EnvironmentVariableTarget]::User)
```

Restart your terminal after updating PATH.

**Linux / macOS:**

```bash
chmod +x azmon-linux-x64
mv azmon-linux-x64 /usr/local/bin/azmon
```

---

## Commands

### `azmon config`

```bash
azmon config --pat <token>              # Set global PAT
azmon config --interval <seconds>       # Set polling interval (minimum: 10, default: 300)

# Quiet hours — suppress polling during off-hours
azmon config --quiet on|off             # Enable or disable quiet hours
azmon config --quiet-start HH:mm        # Set start time (24h, e.g. 18:00)
azmon config --quiet-end HH:mm          # Set end time   (24h, e.g. 08:00)
azmon config --quiet-zone <tz>          # Set timezone (IANA or Windows ID, default: Asia/Taipei)
```

**Default quiet hours:** 18:00–08:00 Asia/Taipei (enabled by default).

Quiet hours span midnight — during this period auto-polling is suspended. `Ctrl+R` still forces a single immediate poll. The dashboard caption shows `Quiet hours active (18:00–08:00)` instead of the countdown timer.

### `azmon add`

```bash
azmon add <organization> <project> <definitionId> [options]

Options:
  -n, --name <name>   Custom display name
  --pat <token>       Pipeline-specific PAT (overrides global)
```

The `<definitionId>` is the integer in the Azure DevOps pipeline URL: `.../_build?definitionId=42`.

### `azmon remove`

```bash
azmon remove --id <guid>                                         # from azmon list
azmon remove --org <org> --project <project> --definition <id>
```

### `azmon list`

List all configured pipelines with their IDs and PAT source.

### `azmon columns`

```bash
azmon columns                                   # show visibility status
azmon columns --show Organization,Project       # show hidden columns
azmon columns --hide TriggeredBy                # hide a column
azmon columns --reset                           # restore defaults
```

Available: `Pipeline` `Organization` `Project` `Build` `Branch` `Status` `Stages` `Result` `Duration` `TriggeredBy` `LastPoll`

Default visible: `Pipeline` `Build` `Branch` `Status` `Stages` `Result` `Duration` `LastPoll`

### `azmon watch`

```bash
azmon watch               # use configured interval
azmon watch -i <seconds>  # override interval for this session only
```

| Key      | Action                  |
|----------|-------------------------|
| `Ctrl+R` | Force immediate refresh |
| `F5`     | Force immediate refresh |
| `Ctrl+C` | Exit                    |

The dashboard runs in an alternate screen buffer — exiting restores your original terminal content.

---

## Configuration File

`~/.azuremonitor/config.json` (Windows: `C:\Users\<user>\.azuremonitor\config.json`)

Created automatically on first use. The **Build** column in the dashboard is a clickable hyperlink in [supported terminals](#supported-terminals).

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

---

## Azure DevOps PAT Setup

Required scope — **Build → Read** only. All other scopes can be left unchecked.

1. Go to `https://dev.azure.com/<organization>`
2. Profile picture (top-right) → **Personal access tokens** → **New Token**
3. Scopes: **Custom defined** → check **Build → Read**
4. Click **Create** and copy the token immediately (shown only once)

> **Multi-organization:** PATs are scoped per organization. Use `--pat` when adding pipelines from a different org than your global PAT.

---

## Polling Behavior

- Pipelines in the same organization/project are batched into one API request per poll cycle
- When a pipeline is `Running`, one additional Timeline API call fetches stage progress
- On HTTP 429, exponential backoff per org/project group: `5s → 10s → 20s → 40s → 80s → 160s → 300s`
- Display refreshes every second (countdown); data updates only on poll or manual refresh

---

## Supported Terminals

All terminals support basic functionality. Clickable links in the **Build** column require OSC 8:

| Terminal         | Clickable Links | Alternate Screen |
|------------------|:-:|:-:|
| Windows Terminal | Yes | Yes |
| iTerm2 (macOS)   | Yes | Yes |
| GNOME Terminal   | Yes | Yes |
| Alacritty        | Yes | Yes |
| cmd.exe          | No  | No  |
| PowerShell ISE   | No  | No  |

## Supported Operating Systems

| OS                   | Architecture |
|----------------------|--------------|
| Windows 10 / 11      | x64          |
| Windows Server 2019+ | x64          |
| Ubuntu 20.04+        | x64          |
| Debian 11+           | x64          |
| macOS 12+            | x64, ARM64   |

## Build from Source

Requires [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).

```bash
git clone https://github.com/kkman021/PipelineMonitor.git
cd PipelineMonitor
dotnet publish src/AzureSummary.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish/
```

Replace `win-x64` with: `linux-x64`, `osx-x64`, `osx-arm64`.
