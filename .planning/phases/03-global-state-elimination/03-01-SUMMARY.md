---
phase: 03-global-state-elimination
plan: "01"
subsystem: DI wiring / options / deprecation markers
tags: [options, iOptions, DI, obsolete, LogViewerOptions, STATE-01, STATE-02, STATE-03, STATE-04]
dependency_graph:
  requires: []
  provides: [LogViewerOptions, IOptions<LogViewerOptions>, sink.Options, Obsolete markers]
  affects: [LogEventArgs, LogControlViewModel, AddLogViewerCore]
tech_stack:
  added: [Microsoft.Extensions.Options (IOptions<T>, AddOptions, Configure<T>)]
  patterns: [options pattern, static fallback, nullable options guard]
key_files:
  created: [LogViewer/LogViewerOptions.cs]
  modified:
    - LogViewer/IBaseLoggerSink.cs
    - LogViewer/BaseLoggerSink.cs
    - LogViewer/BaseLoggerLoggingBuilderExtensions.cs
    - LogViewer/BaseLoggerProviderOptions.cs
    - LogViewer/LogEventArgs.cs
    - LogViewer/LogControlViewModel.cs
    - LogViewer/BaseLogger.Settings.cs
    - LogViewer.Tests/TestBaseLoggerSink.cs
    - LogViewer.Tests/BaseLoggerLoggingBuilderExtensionsTests.cs
    - LogViewerExample/App.xaml.cs
decisions:
  - "LogViewerOptions uses renamed properties (MaxLogQueueSize, LogDateTimeFormat, LogUTCTime) aligned with BaseLogger static names rather than BaseLoggerProviderOptions aliases"
  - "BaseLoggerProviderOptions retained as [Obsolete] type — not deleted — so existing consumer code gets CS0618 warning not a compile error"
  - "Tests for DateTimeFormat and UtcTime updated to assert sink.Options instead of BaseLogger statics since DI path no longer mutates statics"
metrics:
  duration_seconds: 236
  completed_date: "2026-05-17"
  tasks_completed: 3
  files_changed: 10
---

# Phase 03 Plan 01: DI Wiring — LogViewerOptions, IOptions Registration, Remove Static Mutations, Obsolete Markers Summary

**One-liner:** Introduced `LogViewerOptions` as the canonical DI-wired options POCO registered as `IOptions<LogViewerOptions>`, removed all static configuration mutations from the DI path, and marked the inheritance-pattern entry points obsolete with CS0618 warnings.

## What Was Done

### Task 1 — LogViewerOptions POCO + IBaseLoggerSink Options property

- Created `LogViewer/LogViewerOptions.cs` — public class (not struct, required for `IOptions<T>`) with 8 properties, each with XML doc comments:
  - `MaxLogQueueSize` (int, 10000)
  - `LogDateTimeFormat` (string, "yyyy-MM-dd HH:mm:ss.fff (zzz)")
  - `LogUTCTime` (bool, false)
  - `ExcludeCharsFromHandle` (ICollection<char>, ['.', '-', ' '])
  - `LogExportFormat` (string, DefaultLogExportFormat)
  - `CategoryColors` (Dictionary<string, LogColor>, OrdinalIgnoreCase)
  - `MinimumLevel` (LogLevel, Trace)
  - `StripNamespaceFromCategory` (bool, true)
- Added `LogViewerOptions? Options { get; set; }` to `IBaseLoggerSink` (with XML doc)
- Added `public LogViewerOptions? Options { get; set; }` auto-property to `BaseLoggerSink`
- Added `public LogViewerOptions? Options { get; set; }` to `TestBaseLoggerSink`

### Task 2 — DI wiring, remove static mutations, Obsolete markers

- Rewrote `AddLogViewerCore` to accept `Action<LogViewerOptions>`:
  - All 4 `AddLogViewer` overloads updated to `Action<LogViewerOptions>` signatures
  - `sink.Options = options;` set immediately after sink is obtained
  - `sink.MaxQueueSize = options.MaxLogQueueSize;` (renamed property)
  - `builder.Services.AddOptions()` + `builder.Services.Configure<LogViewerOptions>(o => configure(o))` registers `IOptions<LogViewerOptions>` for DI injection and appsettings binding
  - **Deleted:** `BaseLogger.LogDateTimeFormat = options.DateTimeFormat` and `BaseLogger.LogUTCTime = options.UseUtcTime` — static mutations gone from DI path
- Marked `BaseLoggerProviderOptions` with `[Obsolete("Use LogViewerOptions instead. ...", false)]` — file retained
- Updated `LogEventArgs`:
  - `LogDateTimeFormatted` → `BaseLoggerSink.Instance.Options?.LogDateTimeFormat ?? BaseLogger.LogDateTimeFormat`
  - `ToString()` → same pattern
  - `FormatLogMessage` null-format branch → `BaseLoggerSink.Instance.Options?.LogExportFormat ?? BaseLogger.LogExportFormat`
- Updated `LogControlViewModel`: removed `= BaseLogger.MaxLogQueueSize` initializer from `MaxLogSize`; added `MaxLogSize = _sink.Options?.MaxLogQueueSize ?? BaseLogger.MaxLogQueueSize;` in constructor body after `_sink` assignment
- Added `[Obsolete("Use builder.AddLogViewer() with the DI pattern. See the migration guide in README.md.", false)]` to `Initialize`, `Shutdown`, `CreateLogger`, `CreateLogger<T>` in `BaseLogger.Settings.cs`

### Task 3 — Full build + test verification (with auto-fixes)

Build revealed 5 compile errors in the test project and example caused by the property renames. Auto-fixed (Rule 1):

- `BaseLoggerLoggingBuilderExtensionsTests.cs`:
  - `options.MaxQueueSize` → `options.MaxLogQueueSize`
  - `options.DateTimeFormat` → `options.LogDateTimeFormat`
  - `options.UseUtcTime` → `options.LogUTCTime`
  - `AddLogViewer_WithDateTimeFormat_AppliesFormat` — assertion updated from `BaseLogger.LogDateTimeFormat` (static, no longer mutated) to `BaseLoggerSink.Instance.Options?.LogDateTimeFormat`
  - `AddLogViewer_WithUtcTime_AppliesUtcSetting` — assertion updated from `BaseLogger.LogUTCTime` to `BaseLoggerSink.Instance.Options?.LogUTCTime`
  - Null-configure test cast updated from `Action<BaseLoggerProviderOptions>` to `Action<LogViewerOptions>`
- `LogViewerExample/App.xaml.cs`: `options.MaxQueueSize` → `options.MaxLogQueueSize`

## Files Created / Modified

| File | Change |
|------|--------|
| `LogViewer/LogViewerOptions.cs` | Created — 8-property options POCO |
| `LogViewer/IBaseLoggerSink.cs` | Added `LogViewerOptions? Options { get; set; }` |
| `LogViewer/BaseLoggerSink.cs` | Implemented `Options` auto-property |
| `LogViewer/BaseLoggerLoggingBuilderExtensions.cs` | Full rewrite — LogViewerOptions signatures, IOptions registration, static mutations removed |
| `LogViewer/BaseLoggerProviderOptions.cs` | Added `[Obsolete]` attribute |
| `LogViewer/LogEventArgs.cs` | 3 read-sites updated to Options-with-fallback pattern |
| `LogViewer/LogControlViewModel.cs` | MaxLogSize initialised from sink.Options in constructor |
| `LogViewer/BaseLogger.Settings.cs` | 4 `[Obsolete]` markers added |
| `LogViewer.Tests/TestBaseLoggerSink.cs` | Added `Options` property |
| `LogViewer.Tests/BaseLoggerLoggingBuilderExtensionsTests.cs` | Updated for renamed properties + updated assertions |
| `LogViewerExample/App.xaml.cs` | MaxQueueSize → MaxLogQueueSize |

## Test Count Before / After

- Before: 125 passing
- After: 125 passing (0 regressions)

## Verification Output

```
Build succeeded — 0 Error(s) — net8.0-windows
Build succeeded — 0 Error(s) — net10.0-windows
Passed! - Failed: 0, Passed: 125, Skipped: 0, Total: 125
```

Grep verifications:
- `BaseLogger.LogDateTimeFormat =` in extensions: 0 matches (PASS)
- `BaseLogger.LogUTCTime =` in extensions: 0 matches (PASS)
- `sink.Options =` in extensions: 1 match (PASS)
- `Configure<LogViewerOptions>` in extensions: 1 match (PASS)
- `[Obsolete]` in BaseLogger.Settings.cs: 4 matches (PASS)

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Test and example code used renamed property names**
- **Found during:** Task 3 full build
- **Issue:** `BaseLoggerLoggingBuilderExtensionsTests.cs` referenced `MaxQueueSize`, `DateTimeFormat`, `UseUtcTime` (old `BaseLoggerProviderOptions` names); `App.xaml.cs` used `MaxQueueSize`
- **Fix:** Updated to `MaxLogQueueSize`, `LogDateTimeFormat`, `LogUTCTime` (new `LogViewerOptions` names); updated test assertions from static reads to `sink.Options` property reads since DI path no longer mutates statics
- **Files modified:** `LogViewer.Tests/BaseLoggerLoggingBuilderExtensionsTests.cs`, `LogViewerExample/App.xaml.cs`
- **Commit:** bbb09e7

## Known Stubs

None — all properties are fully wired with real defaults and DI registration.

## Commits

| Hash | Message |
|------|---------|
| a0dc076 | feat(03-01): add LogViewerOptions POCO and Options property on IBaseLoggerSink |
| eafacb6 | feat(03-01): wire LogViewerOptions into DI, remove static mutations, mark obsolete methods |
| bbb09e7 | fix(03-01): update tests and example for renamed LogViewerOptions properties |

## Self-Check: PASSED

- [x] `LogViewer/LogViewerOptions.cs` exists
- [x] `IBaseLoggerSink` has `LogViewerOptions? Options { get; set; }`
- [x] `BaseLoggerSink` implements `Options`
- [x] `TestBaseLoggerSink` implements `Options`
- [x] `AddLogViewerCore` sets `sink.Options = options`
- [x] `builder.Services.Configure<LogViewerOptions>` present
- [x] No static mutations in `BaseLoggerLoggingBuilderExtensions.cs`
- [x] 4 `[Obsolete]` on Initialize, Shutdown, CreateLogger, CreateLogger<T>
- [x] `BaseLoggerProviderOptions` marked `[Obsolete]` (not deleted)
- [x] 125/125 tests passing
- [x] Commits a0dc076, eafacb6, bbb09e7 exist in git log
