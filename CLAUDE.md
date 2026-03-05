# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

A Windows console app that automates NinjaTrader 8 login and strategy enabling using UI Automation (FlaUI). Single-file C# app with top-level statements. Launches NinjaTrader (aborts if already running), fills in credentials, clicks login, selects live or simulation mode, then finds the Control Center and enables all strategies.

## Build & Run

```bash
dotnet build TreyThomasCodes.Nt8Automater.sln
dotnet run --project TreyThomasCodes.Nt8Automater
```

Targets `net8.0-windows`. Requires Windows for both building and running (FlaUI uses Windows UI Automation).

## Configuration

All configuration via environment variables (or path as first CLI arg):

- `NT8A_USER` - NT username
- `NT8A_PASS` - NT password
- `NT8A_PATH` - (optional) path to NinjaTrader.exe. Defaults to `C:\Program Files\NinjaTrader 8\bin\NinjaTrader.exe`.
- `NT8A_LIVE` - (optional) `TRUE` for live trading, `FALSE` for sim. Omit for multi-provider mode where no mode selector appears.
- `NT8A_CONN_DELAY` - (optional) seconds to wait for connections after login before enabling strategies. Defaults to `10`.

## Key Dependency

**FlaUI.UIA3 v5.0.0** - UI Automation library. Elements are located by AutomationId (e.g., `tbUserName`, `passwordBox`, `btnLogin`, `btnLiveTrading`, `btnSimulation`). Uses `Retry.WhileNull()` for waiting on post-login dialogs.

## License

MPL-2.0. Source files include SPDX headers.
