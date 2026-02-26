<!--
SPDX-FileCopyrightText: � 2024 Trey Thomas <trey@treythomas.codes>
	
SPDX-License-Identifier: MPL-2.0
-->

# nt8-autologin
Quick and dirty app to automatically launch and log into NinjaTrader 8.

> **Use at your own risk.** This software is provided as-is with no warranties. You are a chode if you run software like this from the internet without auditing it and understanding it. It interacts with your trading software, and you are solely responsible for any consequences of its use.

## Environment Vars

* NT8A_USER - NT username
* NT8A_PASS - NT password
* NT8A_PATH - (optional) path to NinjaTrader.exe. Defaults to `C:\Program Files\NinjaTrader 8\bin\NinjaTrader.exe`.
* NT8A_LIVE - (optional) TRUE for live trading, FALSE for sim. Omit for multi-provider mode (no mode selector).
