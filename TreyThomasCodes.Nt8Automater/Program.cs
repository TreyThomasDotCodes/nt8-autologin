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

