# NinjaTrader 8 Strategy Auto-Start Design

## Problem

NinjaTrader 8 does not support auto-starting strategies after the application launches. Strategies must be manually enabled by checking the "Enabled" checkbox. For an unattended server running 3-5 fixed strategies on charts, this requires human intervention after every restart.

## Approach: External UI Automation (UIA3)

Extend the existing C#/.NET UIA3 application that already launches NT8 and handles login. Add a new phase that finds the Control Center, verifies the data connection, and enables strategies via the Strategies tab.

### Why This Approach

- Builds on existing UIA3 infrastructure (launch + login already implemented)
- Does not touch NT8 internals (no reflection, no unsupported NinjaScript APIs)
- Strategies remain unmodified - no NinjaScript changes needed
- If NT8 UI changes on update, only the automation selectors need updating, not trading code
- External watchdog process is an advantage for an unattended server

### Alternatives Considered

1. **NT8 Addon with Reflection** - Extremely fragile, relies on private internals that change between versions, complex reverse-engineering required. Rejected due to maintenance burden.
2. **NT8 Addon with WPF Visual Tree Walking** - Less fragile than reflection but still unsupported, requires addon development experience, threading complexity. Rejected in favor of extending existing external automation.

## Architecture

### Flow

```
[Launch NT8] -> [Login on Splash Screen] -> [Find Control Center] -> [Verify Connection] -> [Enable Strategies]
     |                  |                          |                        |                       |
  (existing)        (existing)                 (new step)              (new step)              (new step)
```

### Phase 1: Find Control Center Window

After login completes on the NT8 splash screen, the splash closes and the Control Center window appears.

- Poll for the Control Center window by title (e.g., "Control Center") or class name
- Use `AutomationElement.RootElement` search or FlaUI equivalent
- Timeout after configurable duration (e.g., 60 seconds)
- Store window reference for subsequent phases

### Phase 2: Wait for Connection Readiness

Before enabling strategies, wait for the data/broker connection to establish and charts to initialize.

**Current implementation:** A fixed delay controlled by the `NT8A_CONN_DELAY` environment variable (default: 10 seconds). This provides a simple, reliable baseline without requiring knowledge of internal UI element IDs.

**Future enhancement:** Poll the connection status indicator in the Control Center until the data connection shows "Connected" status, then wait an additional configurable buffer (15-30 seconds) for historical data downloads and chart rendering. Timeout if connection doesn't establish within a configurable duration (e.g., 5 minutes). This requires discovering the connection status element's AutomationId via FlaUI Inspect.

### Phase 3: Enable Strategies

Once connections are verified:

1. Click the "Strategies" tab in the Control Center
2. Locate strategy rows in the strategies grid
3. For each strategy: find the Enabled checkbox and check it if unchecked
4. Verify each strategy shows as Enabled after clicking
5. Log success/failure for each strategy

**Strategy identification:** Enable all strategies in the grid. The workspace is pre-configured with only the target strategies, so enable-all is safe and simple.

### Error Handling & Resilience

For unattended server operation:

- **Retry logic:** If a strategy fails to enable, retry up to N times with a delay between attempts
- **Timeouts:** Configurable timeouts at each phase (Control Center discovery, connection, strategy enabling)
- **Logging:** Log every step with timestamps for remote diagnosis
- **Failure alerting:** On timeout or repeated failure, log clearly so monitoring tools can detect the issue
- **Health check (future):** Periodically verify strategies remain enabled (NT8 can disable strategies on errors)

## Technical Details

- **Language:** C# / .NET
- **UI Automation:** UIA3 (via existing project's framework/library)
- **NT8 version:** NinjaTrader 8 (WPF-based desktop application)
- **Target:** Windows, running on unattended server/VPS

## Constraints & Risks

- **Unsupported by NinjaTrader:** This workaround is explicitly not supported. NT8 intentionally requires manual strategy enabling.
- **UI changes:** NT8 updates may change UI element names, automation IDs, or visual tree structure, requiring automation selector updates.
- **Timing sensitivity:** If strategies are enabled before data is fully loaded, they may start in an incorrect state. The connection verification + buffer delay mitigates this.
- **Single connection:** Current design assumes a single data/broker connection. Would need adjustment for multiple connections.

## Open Items

- Exact UIA3 element selectors for Control Center, Strategies tab, and strategy grid need to be mapped by inspecting the live UI tree (using Inspect.exe or FlaUI's tooling)
- Integration point with existing UIA3 app code to be determined when project is shared
