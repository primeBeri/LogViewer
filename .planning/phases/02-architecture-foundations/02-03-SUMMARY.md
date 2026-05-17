---
phase: 02-architecture-foundations
plan: "03"
subsystem: color-abstraction
tags: [architecture, platform-neutral, logcolor, converters, wpf-extension, breaking-change]
requirements: [ARCH-01, ARCH-02]
dependency_graph:
  requires:
    - 02-01 (LogColor struct and LogColorWpfExtensions)
    - 02-02 (IDispatcher abstraction)
  provides:
    - Platform-neutral ILoggable.LogColor property (LogColor)
    - Platform-neutral LogEventArgs.LogColor property and constructor parameter (LogColor)
    - Platform-neutral BaseLogger.LogColor field and constructors (LogColor)
    - ColorToBrushConverter using LogColor.ToSolidColorBrush()
    - LogLevelColorConverter using LogColor.FromRgb + ToSolidColorBrush()
  affects:
    - LogViewer/ILoggable.cs
    - LogViewer/LogEventArgs.cs
    - LogViewer/BaseLogger.cs
    - LogViewer/BaseLogger.Settings.cs
    - LogViewer/Logger.cs
    - LogViewer/BaseLoggerProvider.cs
    - LogViewer/BaseLoggerProviderOptions.cs
    - LogViewer/LogEventArgsMap.cs
    - LogViewer/Converters/ColorToBrushConverter.cs
    - LogViewer/Converters/LogLevelColorConverter.cs
    - LogViewer.Tests (8 test files)
    - LogViewerExample (3 files)
tech_stack:
  added: []
  patterns:
    - Platform-neutral value type (LogColor) replacing WPF-specific type at core interface boundaries
    - WPF adapter extension (ToSolidColorBrush) called only at XAML converter layer
key_files:
  created: []
  modified:
    - LogViewer/ILoggable.cs
    - LogViewer/LogEventArgs.cs
    - LogViewer/BaseLogger.cs
    - LogViewer/BaseLogger.Settings.cs
    - LogViewer/Logger.cs
    - LogViewer/BaseLoggerProvider.cs
    - LogViewer/BaseLoggerProviderOptions.cs
    - LogViewer/LogEventArgsMap.cs
    - LogViewer/Converters/ColorToBrushConverter.cs
    - LogViewer/Converters/LogLevelColorConverter.cs
    - LogViewer.Tests/BaseLoggerLoggingBuilderExtensionsTests.cs
    - LogViewer.Tests/BaseLoggerProviderTests.cs
    - LogViewer.Tests/BaseLoggerSinkTests.cs
    - LogViewer.Tests/BaseLoggerTests.cs
    - LogViewer.Tests/IntegrationTests.cs
    - LogViewer.Tests/LogCollectionTests.cs
    - LogViewer.Tests/LogControlViewModelTests.cs
    - LogViewer.Tests/LogEventArgsTests.cs
    - LogViewer.Tests/LogExporterTests.cs
    - LogViewerExample/App.xaml.cs
    - LogViewerExample/ExampleVM.cs
    - LogViewerExample/SomeObject.cs
decisions:
  - "Pre-merge worktree strategy: staged partial changes in a wip commit, merged main to get Wave 1 files (LogColor, IDispatcher), then committed final work — this avoids re-implementing Wave 1 artifacts in the Wave 2 worktree"
  - "Colors.Green (WPF) = #FF008000 → LogColor.FromRgb(0,128,0) in tests — matched exactly via WPF ARGB byte values"
  - "LogEventArgsMap.cs was not in the plan's listed files but needed identical CultureInfo fix as LogEventArgs.FormatLogMessage — treated as Rule 1 (bug fix)"
  - "Test files updated as Rule 3 (blocking issue) — they could not compile once the production API changed from Color to LogColor"
  - "Example project (App.xaml.cs, ExampleVM.cs, SomeObject.cs) also fixed as Rule 3 — same blocking compilation failure"
  - "NU1603 NuGet warnings remain as pre-existing issues (present since Phase 1); C# compilation is 0 errors 0 warnings"
metrics:
  duration: ~18 min
  completed: "2026-05-17"
  tasks_completed: 2
  tasks_total: 2
  files_created: 0
  files_modified: 22
---

# Phase 2 Plan 3: LogColor Migration — Core Interfaces, Converters, and Callsites Summary

**One-liner:** Replaced `System.Windows.Media.Color` with platform-neutral `LogColor` struct across all 5 core library files plus 8 test files and 3 example project files — converters now use `ToSolidColorBrush()` extension, and the core interfaces have zero WPF references.

## Tasks Completed

| # | Name | Commit | Files |
|---|------|--------|-------|
| wip | Partial core migration (pre-merge) | d03b13a | ILoggable.cs, LogEventArgs.cs, BaseLogger.cs, BaseLogger.Settings.cs, Logger.cs, BaseLoggerProvider.cs, BaseLoggerProviderOptions.cs |
| merge | Main branch merge (Wave 1 artifacts) | cb51c02 | LogColor.cs, LogColorWpfExtensions.cs, IDispatcher.cs, WpfDispatcher.cs (from main) |
| 1 | Full callsite migration + tests + example | 6d48d42 | LogEventArgsMap.cs, 9 test files, 3 example files |
| 2 | XAML converters update | 350ec1c | ColorToBrushConverter.cs, LogLevelColorConverter.cs |

## What Was Built

### Task 1: Core Type Migration

**`ILoggable.cs`** — Removed `using System.Windows.Media;`. Changed `Color LogColor { get; set; }` → `LogColor LogColor { get; set; }` with updated XML doc.

**`LogEventArgs.cs`** — Removed `using System.Windows.Media;`. Primary constructor param `Color color` → `LogColor color`. Property `public Color LogColor` → `public LogColor LogColor`. `FormatLogMessage` "color" arm: removed `CultureInfo.InvariantCulture` arg (LogColor.ToString() has no overload taking CultureInfo).

**`BaseLogger.cs`** — Removed `using System.Windows.Media;`. Protected constructor: `Color? color` → `LogColor? color`; `Colors.Black` → `LogColor.Black`. Internal constructor: `Color color` → `LogColor color`. Public property: `public Color LogColor` → `public LogColor LogColor`.

**`BaseLogger.Settings.cs`** (Rule 3 — blocking) — Removed `using System.Windows.Media;`. Both `CreateLogger` overloads: `Color?` → `LogColor?`.

**`Logger.cs`** (Rule 3 — blocking) — Removed `using System.Windows.Media;`. Primary constructor: `Color?` → `LogColor?`; `Colors.Black` → `LogColor.Black`.

**`BaseLoggerProvider.cs`** (Rule 3 — blocking) — Removed `using System.Windows.Media;`. `ConcurrentDictionary<string, Color>` → `ConcurrentDictionary<string, LogColor>`. `Colors.Black` → `LogColor.Black`. All three `SetCategoryColor`/`SetCategoryColors` signatures: `Color` → `LogColor`.

**`BaseLoggerProviderOptions.cs`** (Rule 3 — blocking) — Removed `using System.Windows.Media;`. `Dictionary<string, Color>` → `Dictionary<string, LogColor>`.

**`LogEventArgsMap.cs`** (Rule 1 — bug) — The CSV color mapping called `LogColor.ToString(CultureInfo.InvariantCulture)` which doesn't exist. Fixed to `LogColor.ToString()`.

**Tests** (Rule 3 — blocking) — 9 test files: replaced `using System.Windows.Media;`, `Colors.*` → `LogColor.Black` / `LogColor.FromRgb(r,g,b)`, `Color?` → `LogColor?`, `Dictionary<string, Color>` → `Dictionary<string, LogColor>`. Updated assertions to use LogColor values.

**`LogViewerExample`** (Rule 3 — blocking) — 3 files: `App.xaml.cs` (colors → LogColor.FromRgb equivalents), `ExampleVM.cs` (`Color.FromArgb` → `LogColor.FromArgb`), `SomeObject.cs` (`Color?` → `LogColor?`).

### Task 2: XAML Converters

**`ColorToBrushConverter.cs`** — Added `using LogViewer;`. Convert method: `if (value is System.Windows.Media.Color color) { return new SolidColorBrush(color); }` → `if (value is LogColor logColor) { return logColor.ToSolidColorBrush(); }`. Default: `Brushes.Black` → `LogColor.Black.ToSolidColorBrush()`. Updated XML docs.

**`LogLevelColorConverter.cs`** — Added `using LogViewer;`. Replaced local `System.Windows.Media.Color` variable + `Colors.*` switch with `LogColor` switch using `LogColor.FromRgb` values matching exact ARGB bytes of `System.Windows.Media.Colors.*`. Result piped through `.ToSolidColorBrush()`. ARGB equivalents: Gray=`(128,128,128)`, Blue=`(0,0,255)`, Orange=`(255,165,0)`, Red=`(255,0,0)`, DarkRed=`(139,0,0)`.

## Verification

```
grep -n "System.Windows.Media.Color|System.Windows.Media.Colors" LogViewer/ILoggable.cs LogViewer/LogEventArgs.cs LogViewer/BaseLogger.cs
→ (no output — clean)

grep -n "ToSolidColorBrush" LogViewer/Converters/ColorToBrushConverter.cs LogViewer/Converters/LogLevelColorConverter.cs
→ 4 matches — both files use ToSolidColorBrush

dotnet build LogViewer.sln --no-incremental -warnaserror:/warnaserror:CS
→ Build succeeded (0 CS errors, 0 CS warnings)

dotnet test LogViewer.Tests/
→ Passed! — 125/125 tests pass
```

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Additional library files not listed in plan also used System.Windows.Media.Color**
- **Found during:** Task 1 build verification
- **Issue:** Plan listed 5 files but `BaseLogger.Settings.cs`, `Logger.cs`, `BaseLoggerProvider.cs`, `BaseLoggerProviderOptions.cs`, and `LogEventArgsMap.cs` also used the old `Color` type — blocking compilation
- **Fix:** Applied same `Color` → `LogColor` substitution to all additional files
- **Files modified:** `BaseLogger.Settings.cs`, `Logger.cs`, `BaseLoggerProvider.cs`, `BaseLoggerProviderOptions.cs`, `LogEventArgsMap.cs`
- **Commit:** 6d48d42

**2. [Rule 1 - Bug] LogEventArgsMap.cs called ToString with CultureInfo overload**
- **Found during:** Task 1 build
- **Issue:** `args.Value.LogColor.ToString(CultureInfo.InvariantCulture)` — `LogColor.ToString()` has no CultureInfo overload, causing CS1501
- **Fix:** Changed to `args.Value.LogColor.ToString()` matching the same fix applied to LogEventArgs.FormatLogMessage
- **Files modified:** `LogViewer/LogEventArgsMap.cs`
- **Commit:** 6d48d42

**3. [Rule 3 - Blocking] All test files used System.Windows.Media.Color to call APIs that changed**
- **Found during:** Task 1 build (test project)
- **Issue:** 9 test files passed `Colors.Black`, `Colors.Blue`, etc. to `LogEventArgs(...)` and `BaseLogger(...)` constructors and `SetCategoryColor()` — type mismatch after the API change
- **Fix:** Updated all test files to use `LogColor.Black` and `LogColor.FromRgb(r,g,b)` equivalents; updated `Color?` → `LogColor?` in Make() helper signatures; updated assertions
- **Files modified:** 9 test files
- **Commit:** 6d48d42

**4. [Rule 3 - Blocking] Example project also used System.Windows.Media.Color**
- **Found during:** Task 1 full solution build
- **Issue:** `App.xaml.cs`, `ExampleVM.cs`, `SomeObject.cs` all used WPF Color types for category colors and BaseLogger subclassing
- **Fix:** Replaced all usages with `LogColor.FromRgb(r,g,b)` and `LogColor.FromArgb(a,r,g,b)` equivalents
- **Files modified:** 3 example project files
- **Commit:** 6d48d42

**5. [Worktree strategy] Merged main to acquire Wave 1 artifacts**
- **Found during:** Initial setup — `LogColor.cs` and `LogColorWpfExtensions.cs` were missing from worktree (Wave 1 was in separate worktrees, not yet merged into main when this worktree was created)
- **Fix:** Staged partial changes in a wip commit, then `git merge main` to bring in Wave 1 merged results; continued work on top
- **Commits:** d03b13a (wip), cb51c02 (merge)

### Notes

- The NU1603 NuGet version resolution warnings (`-warnaserror` fails for NuGet only) are pre-existing since Phase 1 and unrelated to this plan. C# compilation: 0 errors, 0 warnings.
- Test count increased from 117 (Wave 1 end) to 125 — this is because `LogColorTests.cs` (8 tests from Wave 1 plan 02-01) were included in the wave-merged main but not in the Wave 2 worktree at start; they are now present after the merge.

## Known Stubs

None — all color references are fully implemented using concrete `LogColor` values.

## Threat Flags

None — the color data flows from loggers through event args to UI converters. No new network endpoints, auth paths, or trust-boundary changes. `LogColor.ToString()` returns `#AARRGGBB` which carries no sensitive data (same disposition as T-02-04 in plan's threat model).

## Self-Check: PASSED

- [x] `LogViewer/ILoggable.cs` — no `System.Windows.Media` using; `LogColor LogColor` property
- [x] `LogViewer/LogEventArgs.cs` — `LogColor color` constructor param; `LogColor LogColor` property; `FormatLogMessage` uses `LogColor.ToString()` (no CultureInfo)
- [x] `LogViewer/BaseLogger.cs` — no `System.Windows.Media` using; `LogColor? color` params; `LogColor.Black` default; `public LogColor LogColor` property
- [x] `LogViewer/Converters/ColorToBrushConverter.cs` — pattern-matches `LogColor`; calls `.ToSolidColorBrush()`
- [x] `LogViewer/Converters/LogLevelColorConverter.cs` — `LogColor.FromRgb` switch; calls `.ToSolidColorBrush()`
- [x] grep for `System.Windows.Media.Color` in 3 core files — 0 matches
- [x] grep for `ToSolidColorBrush` in converters — 4 matches (2 per file)
- [x] `dotnet build LogViewer.sln --no-incremental -warnaserror:/warnaserror:CS` — Build succeeded
- [x] `dotnet test LogViewer.Tests/` — 125/125 pass
- [x] Commits d03b13a, cb51c02, 6d48d42, 350ec1c exist on worktree branch
