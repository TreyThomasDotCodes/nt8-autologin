/*
 * SPDX-FileCopyrightText: © 2024 Trey Thomas <trey@treythomas.codes>
 *
 * SPDX-License-Identifier: MPL-2.0
 */

using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Tools;
using FlaUI.UIA3;

string path = Environment.GetEnvironmentVariable("NT8A_PATH")
    ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"NinjaTrader 8\bin\NinjaTrader.exe");
if (args.Length > 0)
    path = args[0];

using var automation = new UIA3Automation();
using var app = Application.AttachOrLaunch(new System.Diagnostics.ProcessStartInfo(path));

var window = app.GetMainWindow(automation)
    ?? throw new InvalidOperationException("Could not find NinjaTrader main window.");
var tbUser = window.FindFirstDescendant(cf => cf.ByAutomationId("tbUserName"))?.AsTextBox()
    ?? throw new InvalidOperationException("Could not find username field (tbUserName).");
var tbPass = window.FindFirstDescendant(cf => cf.ByAutomationId("passwordBox"))?.AsTextBox()
    ?? throw new InvalidOperationException("Could not find password field (passwordBox).");
var btnLogin = window.FindFirstDescendant(cf => cf.ByAutomationId("btnLogin"))?.AsButton()
    ?? throw new InvalidOperationException("Could not find login button (btnLogin).");

tbUser.Text = Environment.GetEnvironmentVariable("NT8A_USER")
    ?? throw new InvalidOperationException("NT8A_USER environment variable is not set.");
tbPass.Focus();
tbPass.Text = Environment.GetEnvironmentVariable("NT8A_PASS")
    ?? throw new InvalidOperationException("NT8A_PASS environment variable is not set.");
btnLogin.Focus();
btnLogin.Invoke();

var live = Environment.GetEnvironmentVariable("NT8A_LIVE");
if (!string.IsNullOrEmpty(live))
{
    var btnLive = Retry.WhileNull(() => window.FindFirstDescendant(cf => cf.ByAutomationId("btnLiveTrading")).AsButton(), TimeSpan.FromSeconds(10));
    var btnSim = Retry.WhileNull(() => window.FindFirstDescendant(cf => cf.ByAutomationId("btnSimulation")).AsButton(), TimeSpan.FromSeconds(10));

    if (bool.Parse(live)) btnLive.Result!.Invoke(); else btnSim.Result!.Invoke();
}

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

// --- Phase 2: Wait for connection readiness ---
var delaySec = int.TryParse(Environment.GetEnvironmentVariable("NT8A_CONN_DELAY"), out var d) ? d : 30;
Console.WriteLine($"Waiting {delaySec}s for connections and charts to initialize...");
Thread.Sleep(TimeSpan.FromSeconds(delaySec));
Console.WriteLine("Connection delay complete.");

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
        cb.IsChecked = true;
        enabled++;
        Console.WriteLine($"  {strategyName}: enabled.");
    }
}

Console.WriteLine($"Done. Enabled {enabled} strategy/strategies. {rows.Length - enabled} were already enabled or skipped.");

