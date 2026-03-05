# NT8 Strategy Auto-Start Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Extend the existing NT8 login automater to find the Control Center after login, verify the data connection, and enable all strategies in the Strategies tab.

**Architecture:** After login and live/sim selection (existing code), poll for the Control Center window by title. Once found, verify connection status, navigate to the Strategies tab, and toggle the Enabled checkbox on each strategy row. All new code stays in `Program.cs` as top-level statements, following the existing pattern.

**Tech Stack:** C# / .NET 8.0-windows, FlaUI.UIA3 v5.0.0

---

### Task 1: Find the Control Center Window After Login

**Files:**
- Modify: `TreyThomasCodes.Nt8Automater/Program.cs:37-44` (append after existing code)

After login + live/sim selection, the splash screen closes and the Control Center window appears as a new top-level window. We need to poll for it.

**Step 1: Add Control Center discovery code**

Append after the existing live/sim block (line 44) in `Program.cs`:

```csharp
// --- Phase 1: Find the Control Center window ---
Console.WriteLine("Waiting for Control Center window...");
var controlCenter = Retry.WhileNull(
    () =>
    {
        var windows = app.GetAllTopLevelWindows(automation);
        return windows.FirstOrDefault(w => w.Title.Contains("Control Center"));
    },
    TimeSpan.FromSeconds(120),
    TimeSpan.FromSeconds(2)
);
var ccWindow = controlCenter.Result
    ?? throw new InvalidOperationException("Control Center window did not appear within timeout.");
Console.WriteLine($"Found Control Center: \"{ccWindow.Title}\"");
```

**Key details for implementer:**
- `app.GetAllTopLevelWindows(automation)` returns all windows owned by the NT8 process
- The Control Center window title typically contains "Control Center" — use `Contains` rather than exact match in case NT8 appends version info or account name
- `Retry.WhileNull` polls every 2 seconds for up to 120 seconds — generous timeout for login + workspace loading
- The splash window may still be closing during this period; `GetAllTopLevelWindows` handles that gracefully

**Step 2: Build and verify it compiles**

Run: `dotnet build TreyThomasCodes.Nt8Automater.sln`
Expected: Build succeeded, 0 errors

**Step 3: Commit**

```bash
git add TreyThomasCodes.Nt8Automater/Program.cs
git commit -m "feat: find Control Center window after login"
```

---

### Task 2: Verify Data Connection Status

**Files:**
- Modify: `TreyThomasCodes.Nt8Automater/Program.cs` (append after Task 1 code)

The Control Center shows connection status. We need to wait until the data connection is established before enabling strategies. The connection status appears in the Control Center — the exact UI element will need discovery via FlaUI's element inspection.

**Important note:** The exact AutomationId or element name for the connection status is unknown and must be discovered at runtime on the target machine. We'll implement a flexible approach: first try known patterns, then fall back to a configurable time delay.

**Step 1: Add connection verification with configurable delay**

Append after the Control Center discovery code:

```csharp
// --- Phase 2: Wait for connection readiness ---
var delaySec = int.TryParse(Environment.GetEnvironmentVariable("NT8A_CONN_DELAY"), out var d) ? d : 10;
Console.WriteLine($"Waiting {delaySec}s for connections and charts to initialize...");
Thread.Sleep(TimeSpan.FromSeconds(delaySec));
Console.WriteLine("Connection delay complete.");
```

**Why a simple delay first:** The connection status UI element's AutomationId is unknown without inspecting the live NT8 process. A configurable delay (`NT8A_CONN_DELAY` env var, default 10s) is the reliable baseline. The user can later inspect the UI tree with FlaUI's tooling and add proper connection polling as an enhancement.

**Step 2: Build and verify**

Run: `dotnet build TreyThomasCodes.Nt8Automater.sln`
Expected: Build succeeded, 0 errors

**Step 3: Commit**

```bash
git add TreyThomasCodes.Nt8Automater/Program.cs
git commit -m "feat: add configurable connection delay before strategy enable"
```

---

### Task 3: Navigate to the Strategies Tab and Enable All Strategies

**Files:**
- Modify: `TreyThomasCodes.Nt8Automater/Program.cs` (append after Task 2 code)

The Strategies tab in the Control Center shows a data grid with an "Enabled" checkbox column. We need to:
1. Click the "Strategies" tab
2. Find all strategy rows in the grid
3. For each row, find the Enabled checkbox and check it if unchecked

**Step 1: Add strategy enabling code**

Append after the connection delay code:

```csharp
// --- Phase 3: Navigate to Strategies tab and enable all strategies ---
Console.WriteLine("Looking for Strategies tab...");
var strategiesTab = Retry.WhileNull(
    () => ccWindow.FindFirstDescendant(cf => cf.ByName("Strategies"))
        ?? ccWindow.FindFirstDescendant(cf => cf.ByAutomationId("Strategies")),
    TimeSpan.FromSeconds(30),
    TimeSpan.FromSeconds(2)
);
var tabElement = strategiesTab.Result
    ?? throw new InvalidOperationException("Could not find Strategies tab in Control Center.");

// Click the Strategies tab to make sure it's selected
tabElement.Click();
Console.WriteLine("Strategies tab selected.");
Thread.Sleep(TimeSpan.FromSeconds(2)); // Brief pause for tab content to render

// Find the strategies data grid
var grid = Retry.WhileNull(
    () => ccWindow.FindFirstDescendant(cf => cf.ByControlType(FlaUI.Core.Definitions.ControlType.DataGrid))
        ?? ccWindow.FindFirstDescendant(cf => cf.ByControlType(FlaUI.Core.Definitions.ControlType.Table)),
    TimeSpan.FromSeconds(15),
    TimeSpan.FromSeconds(2)
);
var strategiesGrid = grid.Result
    ?? throw new InvalidOperationException("Could not find strategies grid.");

// Find all rows and enable each strategy
var rows = strategiesGrid.FindAllDescendants(cf => cf.ByControlType(FlaUI.Core.Definitions.ControlType.DataItem));
if (rows.Length == 0)
    rows = strategiesGrid.FindAllDescendants(cf => cf.ByControlType(FlaUI.Core.Definitions.ControlType.Custom));

Console.WriteLine($"Found {rows.Length} strategy row(s).");

var enabled = 0;
foreach (var row in rows)
{
    // Find the Enabled checkbox within this row
    var checkbox = row.FindFirstDescendant(cf => cf.ByControlType(FlaUI.Core.Definitions.ControlType.CheckBox));
    if (checkbox is null)
    {
        Console.WriteLine($"  Row: no checkbox found, skipping.");
        continue;
    }

    var cb = checkbox.AsCheckBox();
    var strategyName = row.Name ?? "unknown";
    if (cb.IsChecked == true)
    {
        Console.WriteLine($"  {strategyName}: already enabled.");
    }
    else
    {
        cb.Click();
        var deadline = DateTime.UtcNow.AddSeconds(2);
        while (cb.IsChecked != true && DateTime.UtcNow < deadline)
            Thread.Sleep(100);

        if (cb.IsChecked == true)
        {
            enabled++;
            Console.WriteLine($"  {strategyName}: enabled.");
        }
        else
        {
            Console.WriteLine($"  WARNING: {strategyName} checkbox did not toggle to enabled.");
        }
    }
}

Console.WriteLine($"Done. Enabled {enabled} strategy/strategies. {rows.Length - enabled} were already enabled or skipped.");
```

**Key details for implementer:**
- Tab discovery: tries both `ByName("Strategies")` and `ByAutomationId("Strategies")` since we don't know the exact identifier
- Grid discovery: tries both `DataGrid` and `Table` control types — WPF apps can use either
- Row discovery: tries `DataItem` then `Custom` — NT8 may use custom row types
- Checkbox: each row should contain one CheckBox for the "Enabled" column
- `cb.Click()` toggles the checkbox via a UI click, followed by a short poll loop to verify the state changed
- `row.Name` may contain the strategy name for logging

**Step 2: Build and verify**

Run: `dotnet build TreyThomasCodes.Nt8Automater.sln`
Expected: Build succeeded, 0 errors

**Step 3: Commit**

```bash
git add TreyThomasCodes.Nt8Automater/Program.cs
git commit -m "feat: enable all strategies in Control Center Strategies tab"
```

---

### Task 4: Update CLAUDE.md with New Environment Variables

**Files:**
- Modify: `CLAUDE.md`

**Step 1: Update the Configuration section**

In `CLAUDE.md`, add the new env var to the Configuration section and update the project description:

Update the **Project Overview** line to:
```
A Windows console app that automates NinjaTrader 8 login and strategy enabling using UI Automation (FlaUI). Single-file C# app with top-level statements. Launches NinjaTrader (aborts if already running), fills in credentials, clicks login, selects live or simulation mode, then finds the Control Center and enables all strategies.
```

Add to the **Configuration** list:
```
- `NT8A_CONN_DELAY` - (optional) seconds to wait for connections after login before enabling strategies. Defaults to `10`.
```

Update the **Key Dependency** version to match csproj:
```
**FlaUI.UIA3 v5.0.0**
```

**Step 2: Commit**

```bash
git add CLAUDE.md
git commit -m "docs: update CLAUDE.md with strategy auto-start details"
```

---

### Task 5: Manual Testing on Target Machine

This task cannot be automated — it requires running against the actual NinjaTrader 8 installation.

**Step 1: Inspect the UI tree**

Before running, use FlaUI Inspect (or Windows Inspect.exe from Windows SDK) on a running NT8 instance to verify:
- The Control Center window title contains "Control Center"
- The Strategies tab element name or AutomationId
- The grid control type (DataGrid vs Table)
- The row control type (DataItem vs Custom)
- The Enabled checkbox is a standard CheckBox control type

**Step 2: Run with verbose output**

```bash
NT8A_USER=your_user NT8A_PASS=your_pass NT8A_LIVE=FALSE NT8A_CONN_DELAY=10 dotnet run --project TreyThomasCodes.Nt8Automater
```

Watch the console output for each phase. If any element isn't found, inspect the UI tree and adjust the selectors in `Program.cs`.

**Step 3: Tune the connection delay**

If strategies are enabled too early (before data loads), increase `NT8A_CONN_DELAY`. If 10s is too long, decrease it.

**Step 4: Verify strategies are running**

Check the NT8 Strategies tab — all strategies should show green (enabled/running).

---

## Known Limitations & Future Improvements

1. **Element selectors are best-guess** — the exact AutomationIds, control types, and element names for the Control Center Strategies tab are unknown without inspecting a live NT8 instance. Task 5 is critical for validating and adjusting selectors.

2. **Connection delay vs. connection polling** — the initial implementation uses a fixed delay. A future improvement could poll the actual connection status indicator once its AutomationId is discovered.

3. **Right-click context menu** — if the checkbox approach doesn't work, the NT8 Strategies tab supports right-clicking a strategy and selecting "Enable" from the context menu. This is an alternative interaction pattern if needed.

4. **Strategy-specific enabling** — current design enables ALL strategies. A future `NT8A_STRATEGIES` env var could accept a comma-separated list of strategy names to enable selectively.
