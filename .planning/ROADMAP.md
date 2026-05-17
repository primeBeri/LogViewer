# Roadmap: LogViewer (RealTimeLogStream)

## Overview

Four phases transform the current `net10.0-windows`-only library (score 67/100) into a v1.0.0 NuGet-ready WPF log viewer that .NET 8 and .NET 10 applications can both install. Phase 1 removes the NuGet compatibility blocker and export bug. Phase 2 decouples WPF types from core interfaces and caps memory growth. Phase 3 replaces global static state with DI-native options and unlocks ViewModel unit tests. Phase 4 removes the Newtonsoft.Json dependency and completes the UI surface.

## Phases

- [x] **Phase 1: Critical Blockers** - Multi-target the library for net8/net10, fix the export bug, and correct CI
- [x] **Phase 2: Architecture Foundations** - Decouple WPF Color from core interfaces, add IDispatcher abstraction, cap pause buffer
- [x] **Phase 3: Global State Elimination** - Replace static BaseLogger settings with IOptions<LogViewerOptions>, mark inheritance pattern obsolete, add VM tests (completed 2026-05-17)
- [x] **Phase 4: Polish and Dependencies** - Remove Newtonsoft.Json, implement or remove LogWindow, add theme support (completed 2026-05-17)

## Phase Details

### Phase 1: Critical Blockers
**Goal**: The NuGet package installs successfully into a .NET 8 or .NET 10 WPF application, the export result flag is correct, and CI builds both TFMs.
**Depends on**: Nothing (first phase)
**Requirements**: TFM-01, TFM-02, TFM-03, BUG-01, CI-01, CI-02
**Estimated effort**: ~1 day (per architecture review)
**Success Criteria** (what must be TRUE):
  1. A net8.0-windows WPF project can add a NuGet reference to RealTimeLogStream without a compatibility error
  2. A net10.0-windows WPF project can add a NuGet reference without a compatibility error
  3. The published NuGet package contains both `lib/net8.0-windows/` and `lib/net10.0-windows/` output folders
  4. `ExportLogResult.Success` is `true` after a successful log export; callers can reliably detect success vs failure
  5. The CI pipeline (`nuget-publish.yml`) completes both build and publish steps without SDK version errors
**Plans**: 4 plans
Plans:
- [x] 01-01-PLAN.md — Multi-target csproj: switch to TargetFrameworks, add conditional Microsoft.Extensions.* package versions
- [x] 01-02-PLAN.md — Fix ExportLogResult.Success bug: set output.Success = true on the happy path
- [x] 01-03-PLAN.md — Update CI workflow: dual SDK setup-dotnet@v4, PowerShell publish loop
- [x] 01-04-PLAN.md — Update README framework documentation to list both net8.0-windows and net10.0-windows
**UI hint**: no

### Phase 2: Architecture Foundations
**Goal**: Core logging interfaces contain no WPF types, LogControlViewModel can be constructed in tests without a WPF runtime, and the pause buffer cannot grow without bound.
**Depends on**: Phase 1
**Requirements**: ARCH-01, ARCH-02, ARCH-03, ARCH-04, BUG-02
**Estimated effort**: ~3.5 days (per architecture review)
**Success Criteria** (what must be TRUE):
  1. `ILoggable` and `LogEventArgs` reference `LogColor` (a platform-neutral struct) rather than `System.Windows.Media.Color`; the project compiles without `System.Windows.Media` in core interface files
  2. A WPF extension method `LogColor.ToSolidColorBrush()` exists and is used by XAML converters so WPF rendering is unchanged
  3. `LogControlViewModel` accepts `IDispatcher` rather than `Dispatcher`; a test can instantiate it with a mock dispatcher without a WPF runtime
  4. Under sustained high-throughput logging while paused, the `_pauseBuffer` stops growing at `MaxLogSize` entries; memory usage is bounded
**Plans**: 3 plans
Plans:
- [x] 02-01-PLAN.md — Create LogColor struct and LogColorWpfExtensions (platform-neutral color type + WPF extension)
- [x] 02-02-PLAN.md — Create IDispatcher/WpfDispatcher, update LogControlViewModel and LogControl, fix BUG-02 pause buffer cap
- [x] 02-03-PLAN.md — Swap System.Windows.Media.Color for LogColor in ILoggable, LogEventArgs, BaseLogger, and XAML converters
**UI hint**: no

### Phase 3: Global State Elimination
**Goal**: All shared library configuration flows through DI-injected `IOptions<LogViewerOptions>` in the DI code path; the inheritance pattern emits compiler warnings; and `LogControlViewModel` instance behaviour is covered by unit tests that run in any test runner.
**Depends on**: Phase 2
**Requirements**: STATE-01, STATE-02, STATE-03, STATE-04, TEST-01, TEST-02, TEST-03, TEST-04
**Estimated effort**: ~3.5 days (per architecture review)
**Success Criteria** (what must be TRUE):
  1. `LogViewerOptions` consolidates `MaxLogQueueSize`, `LogDateTimeFormat`, `LogUTCTime`, `ExcludeCharsFromHandle`, and category colour map; none of these properties are read from `BaseLogger.*` statics in the DI path
  2. Consumers can configure the library via `Configure<LogViewerOptions>()` or `appsettings.json` without touching static properties
  3. Calling `BaseLogger.Initialize()`, `BaseLogger.Shutdown()`, or the static factory methods produces a compiler warning (`CS0618`) directing to `builder.AddLogViewer()`
  4. The test suite includes `LogControlViewModel` instance-level tests for pause/resume buffer flushing, handle and level filter updates, collection trimming, and `LogExporter` output format — and all pass without a WPF runtime
**Plans**: 2 plans
Plans:
- [x] 03-01-PLAN.md — Create LogViewerOptions, wire IOptions into AddLogViewer, remove static mutations, mark Obsolete entry points
- [x] 03-02-PLAN.md — Add LogControlViewModel instance tests for TEST-01/02/03; confirm TEST-04 coverage
**UI hint**: no

### Phase 4: Polish and Dependencies
**Goal**: The NuGet package carries no Newtonsoft.Json dependency, `LogWindow` is a functional standalone window (or is removed), and `LogControl` inherits the host application's WPF theme.
**Depends on**: Phase 3
**Requirements**: PKG-01, PKG-02, UI-01, UI-02
**Estimated effort**: ~2.5 days (per architecture review)
**Success Criteria** (what must be TRUE):
  1. `Newtonsoft.Json` does not appear in the package's dependency graph; JSON export uses `System.Text.Json.JsonSerializer` and produces equivalent output
  2. `LogWindow` either hosts a working `LogControl` (a consumer can call `new LogWindow().Show()` and see the log viewer) or is absent from the package entirely — it does not ship as an empty `<Grid/>` shell
  3. Embedding `LogControl` in a WPF application with a dark theme renders with a dark background; `Background="White"` is no longer hardcoded in `LogControl.xaml`
**Plans**: 2 plans
Plans:
- [x] 04-01-PLAN.md — Remove Newtonsoft.Json, migrate LogExporter to System.Text.Json, update LogExporterTests assertions
- [x] 04-02-PLAN.md — Embed LogControl in LogWindow.xaml; fix LogControl.xaml hardcoded white background
**UI hint**: yes

## Progress

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 1. Critical Blockers | 4/4 | Complete | 2026-05-17 |
| 2. Architecture Foundations | 3/3 | Complete | 2026-05-17 |
| 3. Global State Elimination | 2/2 | Complete   | 2026-05-17 |
| 4. Polish and Dependencies | 2/2 | Complete   | 2026-05-17 |
