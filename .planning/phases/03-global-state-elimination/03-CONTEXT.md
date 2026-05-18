# Phase 3: Global State Elimination - Context

**Gathered:** 2026-05-17
**Status:** Ready for planning
**Mode:** Auto-generated (infrastructure phase — all success criteria are purely technical)

<domain>
## Phase Boundary

Replace static `BaseLogger.*` configuration with DI-injected `IOptions<LogViewerOptions>`, mark the inheritance pattern entry points obsolete, and add instance-level unit tests for `LogControlViewModel` and `LogExporter`.

Specifically:
- Create `LogViewerOptions` POCO consolidating all shared config (currently scattered across `BaseLogger.Settings.cs` static properties and `BaseLoggerProviderOptions`)
- Register `LogViewerOptions` as `IOptions<LogViewerOptions>` in `AddLogViewer()`; expose config via `appsettings.json` / `Configure<LogViewerOptions>()` without touching statics
- Mark `BaseLogger.Initialize()`, `BaseLogger.Shutdown()`, `BaseLogger.CreateLogger()`, `BaseLogger.CreateLogger<T>()` with `[Obsolete("Use builder.AddLogViewer(). See migration guide.", false)]`
- Remove the static mutation calls (`BaseLogger.LogDateTimeFormat = ...`, `BaseLogger.LogUTCTime = ...`) from `AddLogViewerCore` so the DI path reads config from `IOptions<LogViewerOptions>` rather than global statics
- Add `LogControlViewModel` instance tests for pause/resume, handle/level filtering, and collection trimming
- Verify `LogExporter` tests cover JSON, CSV, and plain-text output (tests already exist; gap-fill only if needed)

</domain>

<decisions>
## Implementation Decisions

### Claude's Discretion
All implementation choices are at Claude's discretion — pure infrastructure phase. Use ROADMAP phase goal, success criteria, and codebase conventions to guide decisions.

Key constraints from REQUIREMENTS.md and ROADMAP:
- `LogViewerOptions` must expose: `MaxLogQueueSize` (int, default 10000), `LogDateTimeFormat` (string, default "yyyy-MM-dd HH:mm:ss.fff (zzz)"), `LogUTCTime` (bool, default false), `ExcludeCharsFromHandle` (ICollection<char>, default ['.', '-', ' ']), `CategoryColors` (Dictionary<string, LogColor>)
- `IOptions<LogViewerOptions>` must be registered so consumers can inject it or configure via `appsettings.json`
- `AddLogViewer()` overloads that currently accept `Action<BaseLoggerProviderOptions>` should accept `Action<LogViewerOptions>` — since this is pre-v1.0, renaming is fine (no released public API to break)
- `BaseLoggerProviderOptions` can be kept as a `[Obsolete]` type delegating to `LogViewerOptions`, or removed; prefer removal since nothing external consumes it yet
- The static properties on `BaseLogger` (`LogDateTimeFormat`, `LogUTCTime`, `MaxLogQueueSize`, `ExcludeCharsFromHandle`) must remain for the inheritance pattern (they are not removed, only their reads in the DI path are replaced)
- `LogEventArgs.LogDateTimeFormatted` currently reads `BaseLogger.LogDateTimeFormat`. To break the static read in the DI path, store the effective options in `BaseLoggerSink` (set during `AddLogViewerCore`) and read from there with a static fallback: `BaseLoggerSink.Instance.Options?.LogDateTimeFormat ?? BaseLogger.LogDateTimeFormat`
- `LogControlViewModel.MaxLogSize` default currently reads `BaseLogger.MaxLogQueueSize`; change to `_sink.Options?.MaxLogQueueSize ?? BaseLogger.MaxLogQueueSize` (where `_sink` is the injected `IBaseLoggerSink`)
- `IBaseLoggerSink` should expose `LogViewerOptions? Options { get; set; }` so the VM can read it without a static reference

### Test requirements (TEST-01 through TEST-04)
- TEST-01: `LogControlViewModel` tests for `IsPaused` toggle — events buffered when paused, flushed (appear in `LogEvents`) when resumed. Use `FakeDispatcher` + `TestBaseLoggerSink`.
- TEST-02: `LogControlViewModel` tests for handle filter (`HandleFilter` property) and level filter (`LogLevelFilter` property) — filtered events do not appear in `LogEvents`; invalid regex for handle filter is handled gracefully.
- TEST-03: `LogControlViewModel` tests for `AddAndTrimLogEventsIfNeededAsync` — collection is trimmed when `LogEvents.Count > MaxLogSize`.
- TEST-04: `LogExporter` tests are already comprehensive in `LogExporterTests.cs` (17 tests). Gap: no test for the custom format string applied via `LogViewerOptions.LogExportFormat`. Add one additional test that verifies `GetLogsAsTextAsync(events, null)` uses the configured `LogExportFormat` from the injected options path (or at minimum that the custom format is applied when `BaseLogger.LogExportFormat` is set).

</decisions>

<code_context>
## Existing Code Insights

### What exists today
- `BaseLoggerProviderOptions` — options passed to `AddLogViewer(Action<BaseLoggerProviderOptions>)`. Has: `MaxQueueSize`, `MinimumLevel`, `CategoryColors`, `DateTimeFormat`, `UseUtcTime`, `StripNamespaceFromCategory`. DOES NOT HAVE: `ExcludeCharsFromHandle`, `LogExportFormat`.
- `BaseLoggerLoggingBuilderExtensions.AddLogViewerCore` — reads from `BaseLoggerProviderOptions`, then WRITES to static: `BaseLogger.LogDateTimeFormat = options.DateTimeFormat` and `BaseLogger.LogUTCTime = options.UseUtcTime`. This is what STATE-04 removes.
- `BaseLogger.Settings.cs` — all the statics: `MaxLogQueueSize`, `LogDateTimeFormat`, `LogUTCTime`, `ExcludeCharsFromHandle`, `LogExportFormat`, `DefaultLogDisplayFormat`, `Initialize()`, `Shutdown()`, `CreateLogger()`, `CreateLogger<T>()`.
- `IBaseLoggerSink` interface — `Write`, `LogReceived`, `MaxQueueSize`, `LogQueue`. No `Options` property.
- `BaseLoggerSink` — singleton implementation; `MaxQueueSize` maps to `BaseLogger.MaxLogQueueSize`; no options storage.
- `LogEventArgs.LogDateTimeFormatted` — reads `BaseLogger.LogDateTimeFormat` and `BaseLogger.LogUTCTime`.
- `LogControlViewModel.MaxLogSize` — defaults to `BaseLogger.MaxLogQueueSize`.
- `LogControlViewModel` tests — exist for `WildcardToRegex`, pause buffer cap, and constructor. Missing: resume/flush, filter behavior, collection trimming.
- `LogExporterTests` — 17 tests covering JSON, CSV, and text. Uses `Newtonsoft.Json.Linq.JArray` for JSON assertions (will need updating when Phase 4 removes Newtonsoft, but not now).

### Established Patterns
- `ArgumentNullException.ThrowIfNull` at all public API boundaries
- `[Obsolete("message", false)]` for soft deprecation (CS0618 warning, not error)
- `services.Configure<T>()` from `Microsoft.Extensions.Options` for `IOptions<T>` registration
- XML doc comments on all public API members
- `FakeDispatcher` pattern (in `LogControlViewModelTests`) for unit-testing without WPF runtime
- `TestBaseLoggerSink` (in `LogViewer.Tests/TestBaseLoggerSink.cs`) for isolated sink testing

### Files to create
- `LogViewer/LogViewerOptions.cs` — new POCO

### Files to modify (DI wiring)
- `LogViewer/IBaseLoggerSink.cs` — add `LogViewerOptions? Options { get; set; }`
- `LogViewer/BaseLoggerSink.cs` — implement `Options` property
- `LogViewer/BaseLoggerLoggingBuilderExtensions.cs` — rename `BaseLoggerProviderOptions` → `LogViewerOptions` in signatures; register `IOptions<LogViewerOptions>`; remove static writes
- `LogViewer/BaseLoggerProviderOptions.cs` — delete or replace with `[Obsolete]` alias

### Files to modify (static reads)
- `LogViewer/LogEventArgs.cs` — `LogDateTimeFormatted` reads from sink options with fallback
- `LogViewer/LogControlViewModel.cs` — `MaxLogSize` default reads from sink options with fallback

### Files to modify (Obsolete markers)
- `LogViewer/BaseLogger.Settings.cs` — `[Obsolete]` on `Initialize`, `Shutdown`, `CreateLogger`, `CreateLogger<T>`

### Files to modify (tests)
- `LogViewer.Tests/LogControlViewModelTests.cs` — add TEST-01/02/03 tests
- `LogViewer.Tests/LogExporterTests.cs` — add TEST-04 gap test if needed

</code_context>

<specifics>
## Specific Ideas

- `LogViewerOptions` should be a `public class` (not struct) so `IOptions<T>` and configuration binding work correctly
- Use `services.AddOptions()` then `services.Configure<LogViewerOptions>(configure)` in `AddLogViewerCore`
- `IBaseLoggerSink.Options` should be `LogViewerOptions?` (nullable — `null` = use static fallback, for the inheritance pattern)
- `AddLogViewerCore` creates an eager `opts` for the sink's MaxQueueSize and Options, AND registers deferred `IOptions<LogViewerOptions>` for DI injection
- For `LogEventArgs.LogDateTimeFormatted`, the static read change is: `BaseLoggerSink.Instance.Options?.LogDateTimeFormat ?? BaseLogger.LogDateTimeFormat` — this is a "best effort" approach that doesn't require plumbing options through every log event
- The `[Obsolete]` message should say: `"Use builder.AddLogViewer() with the DI pattern. See the migration guide in README.md."`
- `TestBaseLoggerSink` in `LogViewer.Tests/` — check if it needs an `Options` property added to implement the updated `IBaseLoggerSink` interface
- For TEST-01 (resume flush): after filling the buffer and calling `vm.IsPaused = false`, wait briefly and check `vm.LogEvents.Count` matches what was buffered
- For TEST-02 (filter): set `vm.HandleFilter = "specific*"`, write events with matching and non-matching handles, verify `vm.LogEvents` only contains matched entries
- For TEST-03 (trim): set `vm.MaxLogSize = 5`, write 7 events (not paused), verify `vm.LogEvents.Count <= 5`

</specifics>

<deferred>
## Deferred Ideas

None — infrastructure phase, no scope creep possible.

</deferred>
