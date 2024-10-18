/*
 * SPDX-FileCopyrightText: © 2024 Trey Thomas <trey@treythomas.codes>
 *
 * SPDX-License-Identifier: MPL-2.0
 */

using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Tools;
using FlaUI.UIA3;

string path = Environment.GetEnvironmentVariable("NT8A_PATH") ?? "";
if (args.Length > 0)
    path = args[0];

using var automation = new UIA3Automation();
using var app = Application.AttachOrLaunch(new System.Diagnostics.ProcessStartInfo(path));

var window = app.GetMainWindow(automation);
var tbUser = window.FindFirstDescendant(cf => cf.ByAutomationId("tbUserName")).AsTextBox();
var tbPass = window.FindFirstDescendant(cf => cf.ByAutomationId("passwordBox")).AsTextBox();
var btnLogin = window.FindFirstDescendant(cf => cf.ByAutomationId("btnLogin")).AsButton();

tbUser.Text = Environment.GetEnvironmentVariable("NT8A_USER");
tbPass.Focus();
tbPass.Text = Environment.GetEnvironmentVariable("NT8A_PASS");
btnLogin.Focus();
btnLogin.Invoke();

var btnLive = Retry.WhileNull(() => window.FindFirstDescendant(cf => cf.ByAutomationId("btnLiveTrading")).AsButton(), TimeSpan.FromSeconds(10));
var btnSim = Retry.WhileNull(() => window.FindFirstDescendant(cf => cf.ByAutomationId("btnSimulation")).AsButton(), TimeSpan.FromSeconds(10));

if (bool.Parse(Environment.GetEnvironmentVariable("NT8A_LIVE") ?? "")) btnLive.Result.Invoke(); else btnSim.Result.Invoke();

