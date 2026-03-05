<!--
SPDX-FileCopyrightText: © 2024 Trey Thomas <trey@treythomas.codes>

SPDX-License-Identifier: MPL-2.0
-->

# NT8Automater

Automatically launches NinjaTrader 8, logs in, and enables all strategies in the Control Center.

> **Use at your own risk.** This software is provided as-is with no warranties. You are a chode if you run software like this from the internet without auditing it and understanding it. It interacts with your trading software, and you are solely responsible for any consequences of its use.

## What It Does

1. Launches NinjaTrader 8 (aborts if already running)
2. Fills in credentials and clicks Login
3. Selects Live or Simulation mode (if configured)
4. Waits for the Control Center to appear
5. Waits for connections to initialize
6. Navigates to the Strategies tab and enables all strategies

## Environment Vars

* `NT8A_USER` - NT username
* `NT8A_PASS` - NT password
* `NT8A_PATH` - (optional) path to NinjaTrader.exe. Defaults to `C:\Program Files\NinjaTrader 8\bin\NinjaTrader.exe`.
* `NT8A_LIVE` - (optional) `TRUE` for live trading, `FALSE` for sim. Omit for multi-provider mode (no mode selector).
* `NT8A_CONN_DELAY` - (optional) seconds to wait for connections after login before enabling strategies. Defaults to `10`.
