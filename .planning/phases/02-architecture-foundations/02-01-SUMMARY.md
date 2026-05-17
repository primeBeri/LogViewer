---
phase: 02-architecture-foundations
plan: "01"
subsystem: color-abstraction
tags: [architecture, value-type, platform-neutral, wpf-extension, tdd]
dependency_graph:
  requires: []
  provides:
    - LogColor struct (platform-neutral color value type)
    - LogColorWpfExtensions.ToSolidColorBrush (WPF extension)
  affects:
    - Wave 2 plans (02-02) that swap System.Windows.Media.Color for LogColor in ILoggable, LogEventArgs, BaseLogger
tech_stack:
  added: []
  patterns:
    - readonly struct with IEquatable<T> and operator overloads
    - WPF-specific extension class separate from pure C# core type
key_files:
  created:
    - LogViewer/LogColor.cs
    - LogViewer/LogColorWpfExtensions.cs
    - LogViewer.Tests/LogColorTests.cs
  modified: []
decisions:
  - "XML cref for LogColorWpfExtensions.ToSolidColorBrush removed from LogColor.cs remarks (CS1574 forward-reference warning) — plain text reference used instead; cref will resolve correctly once both files are compiled together in subsequent builds"
metrics:
  duration: ~6 min
  completed: "2026-05-17"
  tasks_completed: 2
  tasks_total: 2
  files_created: 3
  files_modified: 0
---

# Phase 2 Plan 1: LogColor Struct and WPF Extension Summary

**One-liner:** Platform-neutral `LogColor` readonly struct with ARGB bytes, factory methods, `#AARRGGBB` ToString, IEquatable, and a WPF-only `ToSolidColorBrush()` extension — foundation for decoupling logging interfaces from `System.Windows.Media`.

## Tasks Completed

| # | Name | Commit | Files |
|---|------|--------|-------|
| RED | Failing LogColor tests | 3c522b2 | LogViewer.Tests/LogColorTests.cs |
| 1 | Create LogColor struct | 57ca371 | LogViewer/LogColor.cs |
| 2 | Create LogColorWpfExtensions | 8bec956 | LogViewer/LogColorWpfExtensions.cs |

## What Was Built

### LogColor.cs

`public readonly struct LogColor : IEquatable<LogColor>` in the `LogViewer` namespace.

- Four public `byte` fields: `A`, `R`, `G`, `B`
- `static LogColor FromArgb(byte a, byte r, byte g, byte b)` — full ARGB factory
- `static LogColor FromRgb(byte r, byte g, byte b)` — opaque (A=255) factory
- `static readonly LogColor Black` = `FromArgb(255, 0, 0, 0)`
- `override string ToString()` returns `$"#{A:X2}{R:X2}{G:X2}{B:X2}"` (e.g. `#FF000000`)
- `IEquatable<LogColor>` with `Equals`, `GetHashCode(HashCode.Combine)`, `==`, `!=`
- Zero WPF/System.Windows references — fully testable without a WPF runtime

### LogColorWpfExtensions.cs

`public static class LogColorWpfExtensions` in the `LogViewer` namespace.

- Single method: `public static SolidColorBrush ToSolidColorBrush(this LogColor logColor)`
- Implementation: `new SolidColorBrush(Color.FromArgb(logColor.A, logColor.R, logColor.G, logColor.B))`
- References `System.Windows.Media` intentionally — this is the WPF adapter boundary

### LogColorTests.cs (8 tests, all passing)

Covers: `FromArgb` channel assignment, `FromRgb` alpha defaulting, `Black` equality, `ToString` format, equality (true/false), operator `==`/`!=`, `GetHashCode` consistency.

## Verification

Full solution build (`dotnet build LogViewer.sln --no-incremental`) succeeds:
- net8.0-windows: 0 errors
- net10.0-windows: 0 errors
- Warnings: pre-existing NU1603 NuGet version conflicts (unrelated to this plan) + pre-existing Fody indexer warning

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Removed forward-reference cref for ToSolidColorBrush in LogColor.cs remarks**
- **Found during:** Task 1 build verification
- **Issue:** CS1574 warning — `<see cref="LogColorWpfExtensions.ToSolidColorBrush"/>` in `LogColor.cs` XML doc couldn't be resolved because `LogColorWpfExtensions` didn't exist yet during single-file compilation
- **Fix:** Changed to plain text `<c>LogColorWpfExtensions.ToSolidColorBrush</c>` to eliminate the warning (which becomes an error under `-warnaserror`)
- **Files modified:** `LogViewer/LogColor.cs`
- **Commit:** included in 57ca371

## Known Stubs

None — both files are complete implementations.

## Threat Flags

None — no new network endpoints, auth paths, or trust-boundary changes. `LogColor` fields are immutable bytes; `ToSolidColorBrush` creates a new WPF brush with no side effects.

## TDD Gate Compliance

- RED gate (test commit): 3c522b2 — `test(02-01): add failing tests for LogColor struct`
- GREEN gate (feat commit): 57ca371 — `feat(02-01): create LogColor platform-neutral color struct`
- Tests: 8/8 passing after GREEN commit

## Self-Check: PASSED

- [x] `LogViewer/LogColor.cs` — exists, compiles, struct has A/R/G/B bytes, FromArgb/FromRgb/Black/ToString
- [x] `LogViewer/LogColorWpfExtensions.cs` — exists, compiles, ToSolidColorBrush extension present
- [x] `LogViewer.Tests/LogColorTests.cs` — exists, 8 tests all passing
- [x] Commit 3c522b2 — RED gate (test)
- [x] Commit 57ca371 — GREEN gate (implementation)
- [x] Commit 8bec956 — Task 2 (WPF extension)
- [x] `dotnet build LogViewer.sln --no-incremental` — 0 errors both TFMs
