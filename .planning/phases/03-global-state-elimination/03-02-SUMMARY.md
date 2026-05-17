---
phase: 03-global-state-elimination
plan: "02"
subsystem: tests / LogControlViewModel instance tests
tags: [tests, TEST-01, TEST-02, TEST-03, TEST-04, pause-resume, filters, trimming, FakeDispatcher, TestBaseLoggerSink]
dependency_graph:
  requires: [03-01 (IDispatcher abstraction, TestBaseLoggerSink.Options)]
  provides: [TEST-01, TEST-02, TEST-03 coverage, TEST-04 confirmation]
  affects: [LogViewer.Tests/LogControlViewModelTests.cs]
tech_stack:
  added: []
  patterns: [FakeDispatcher pattern, TestBaseLoggerSink isolation, inline test setup (no shared state)]
key_files:
  created: []
  modified:
    - LogViewer.Tests/LogControlViewModelTests.cs
decisions:
  - "SetRegexFilterIfValid is internal but accessible via InternalsVisibleTo — called directly in TEST-02d rather than going through the LogHandleFilter setter to test the return value"
  - "TEST-04 confirmed as satisfied by existing Text_NullFormat_FallsBackToBaseLoggerExportFormat — LogExporterTests.cs left unmodified"
  - "All tests use inline setup (no shared fields) to avoid xUnit parallel-execution hazards"
metrics:
  duration_seconds: 180
  completed_date: "2026-05-17"
  tasks_completed: 2
  files_changed: 1
---

# Phase 03 Plan 02: LogControlViewModel Instance Tests (TEST-01/02/03) Summary

**One-liner:** Added 6 instance-level LogControlViewModel tests (pause/resume flush, handle/level filter inclusion/exclusion, invalid regex rejection, collection trimming) using FakeDispatcher + TestBaseLoggerSink — no WPF runtime required.

## What Was Done

### Task 1 — TEST-01 and TEST-02: Pause/Resume and Filters

Added 5 new test methods to `LogViewer.Tests/LogControlViewModelTests.cs` in two new sections after the existing WildcardToRegex section:

**TEST-01 — PauseAndResume_FlushesBufferedEventsToLogEvents**
- Sets `IsPaused = true`, writes 3 events, asserts `LogEvents.Count == 0` (buffered)
- Sets `IsPaused = false`, awaits 50ms, asserts `LogEvents.Count == 3` (flushed)

**TEST-02a — HandleFilter_NonMatchingHandle_NotAddedToLogEvents**
- Sets `LogHandleFilter = WildcardToRegex("specific")`, writes event with handle "other"
- Asserts `LogEvents.Count == 0`

**TEST-02b — HandleFilter_MatchingHandle_AddedToLogEvents**
- Same filter "specific", writes event with handle "specific"
- Asserts `LogEvents.Count == 1`

**TEST-02c — LogLevelFilter_BelowMinimum_NotAddedToLogEvents**
- Sets `LogLevel = LogLevel.Warning`, writes `LogLevel.Information` event
- Asserts `LogEvents.Count == 0`

**TEST-02d — InvalidRegexFilter_ReturnsFalse_FilterUnchanged**
- Calls `vm.SetRegexFilterIfValid("(?!")` — a lookahead pattern rejected by NonBacktracking Regex
- Asserts return value is `false` and `LogHandleFilter` remains `".*"`

### Task 2 — TEST-03: Collection Trimming; TEST-04 Gap Analysis

**TEST-03 — AddLogs_ExceedingMaxLogSize_TrimsCollection**
- Sets `MaxLogSize = 3`, writes 7 events in a loop, awaits 100ms
- Asserts `LogEvents.Count <= 3`

**TEST-04 gap analysis:**
Confirmed `Text_NullFormat_FallsBackToBaseLoggerExportFormat` exists in `LogExporterTests.cs` at line 119. It calls `GetLogsAsTextAsync(events, null)` and asserts output contains "Svc" and "boom" — exercising the null-format fallback branch that reads from `BaseLoggerSink.Instance.Options?.LogExportFormat ?? BaseLogger.LogExportFormat`. No new test required.

## Files Created / Modified

| File | Change |
|------|--------|
| `LogViewer.Tests/LogControlViewModelTests.cs` | +93 lines: 6 new test methods in 3 new sections |

## Test Count Before / After

- Before: 125 passing
- After: 131 passing (6 new, 0 regressions)

## Verification Output

```
Passed! - Failed: 0, Passed: 131, Skipped: 0, Total: 131
```

Grep verifications:
- `IsPaused` in LogControlViewModelTests.cs: 4 matches (PASS — 2 existing, 2 new)
- `LogLevel.Warning` in LogControlViewModelTests.cs: 1 match (PASS)
- `MaxLogSize\s*=\s*3` in LogControlViewModelTests.cs: 1 match (PASS)
- No `System.Windows.*` imports added (PASS)

## Deviations from Plan

None — plan executed exactly as written. `SetRegexFilterIfValid` confirmed as `internal` (accessible via `InternalsVisibleTo`); `LogHandleFilter` confirmed as the correct property name.

## Known Stubs

None — test file only; no UI or data wiring involved.

## Threat Flags

None — pure test code, no new network endpoints, auth paths, or schema changes.

## Commits

| Hash | Message |
|------|---------|
| cd5c5ec | test(03-02): LogControlViewModel instance tests — pause/resume, filters, trimming (TEST-01/02/03) |

## Self-Check: PASSED

- [x] `LogViewer.Tests/LogControlViewModelTests.cs` modified with 6 new test methods
- [x] `PauseAndResume_FlushesBufferedEventsToLogEvents` present (TEST-01)
- [x] `HandleFilter_NonMatchingHandle_NotAddedToLogEvents` present (TEST-02a)
- [x] `HandleFilter_MatchingHandle_AddedToLogEvents` present (TEST-02b)
- [x] `LogLevelFilter_BelowMinimum_NotAddedToLogEvents` present (TEST-02c)
- [x] `InvalidRegexFilter_ReturnsFalse_FilterUnchanged` present (TEST-02d)
- [x] `AddLogs_ExceedingMaxLogSize_TrimsCollection` present (TEST-03)
- [x] TEST-04 confirmed satisfied by existing test — no new test added
- [x] 131/131 tests passing
- [x] Commit cd5c5ec exists in git log
