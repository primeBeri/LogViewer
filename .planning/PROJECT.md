# LogViewer (RealTimeLogStream)

## What This Is

`RealTimeLogStream` is a WPF NuGet library that embeds a real-time, filterable log viewer control directly into WPF applications. It integrates with `Microsoft.Extensions.Logging` as a custom `ILoggerProvider`, letting any WPF application surface all its log traffic — with filtering, pause/resume, and export — by adding a single XAML control and one `AddLogViewer()` call. The library is in active development toward a v1.0 public release on NuGet.org.

## Core Value

A plug-and-play WPF log viewer that any .NET 8 or .NET 10 application can embed via NuGet with a single `AddLogViewer()` call and a `<LogControl />` element — no boilerplate, no friction.

## Requirements

### Validated

These capabilities ship in the current codebase (v0.3.1.0) and are verified by the test suite:

- ✓ Thread-safe `LogCollection` with `HashSet` deduplication and snapshot enumerator — existing
- ✓ `ConcurrentQueue`-based `BaseLoggerSink` with capped replay buffer — existing
- ✓ WPF MVVM with `CommunityToolkit.Mvvm` + `PropertyChanged.Fody` — existing
- ✓ `ILoggerProvider` / DI integration via `builder.AddLogViewer()` — existing
- ✓ `BaseLogger` inheritance integration pattern — existing (to be deprecated)
- ✓ Log export to JSON, CSV, and plain text via `SaveFileDialog` — existing (Success bug present)
- ✓ Handle regex + log level filtering with ReDoS protection — existing
- ✓ Pause / resume log streaming with local buffer — existing
- ✓ WPF `ListView` with virtualised recycling render — existing
- ✓ `ReadOnlyLogCollection` public facade over mutable internal collection — existing
- ✓ Full XML documentation on all public API surface — existing
- ✓ 75+ unit tests covering core components — existing

### Active

All items below target the v1.0.0 public release.

**Phase 1 — Critical Blockers (.NET 8 Readiness)**
- [ ] Library multi-targets `net8.0-windows` and `net10.0-windows` so any .NET 8 or .NET 10 WPF app can install the NuGet package
- [ ] `ExportLogResult.Success` is set to `true` after a successful export (currently always `false`)
- [ ] CI workflow installs the correct .NET SDK(s) to build both target frameworks
- [ ] README accurately documents the supported frameworks

**Phase 2 — Architecture Foundations**
- [ ] `System.Windows.Media.Color` is removed from `ILoggable` and `LogEventArgs`; replaced with a platform-neutral `LogColor` struct (or equivalent) with a WPF-specific extension adapter
- [ ] `IDispatcher` abstraction wraps `System.Windows.Threading.Dispatcher` so `LogControlViewModel` can be unit-tested without a WPF runtime
- [ ] `_pauseBuffer` in `LogControlViewModel` is capped at `MaxLogSize` to prevent unbounded memory growth during long pauses

**Phase 3 — Global State Elimination**
- [ ] `LogViewerOptions` POCO consolidates all shared configuration (`MaxLogQueueSize`, `LogDateTimeFormat`, `LogUTCTime`, etc.) and is registered as `IOptions<LogViewerOptions>` in the DI container
- [ ] `BaseLogger.Initialize()` and the inheritance integration pattern are marked `[Obsolete]` with a migration message pointing to `AddLogViewer()`
- [ ] `LogControlViewModel` instance-level tests run without a WPF runtime, covering `IsPaused`, filter updates, and trim logic

**Phase 4 — Polish and Dependencies**
- [ ] `Newtonsoft.Json` replaced by `System.Text.Json` (BCL, no extra NuGet dependency)
- [ ] `LogWindow` is either implemented as a functional standalone window (embedding `LogControl`) or removed from the package entirely
- [ ] `LogControl` respects the host application's WPF theme (dark/light) by removing hardcoded `Background="White"` and using dynamic resource keys

### Out of Scope

- Real-time remote log streaming (network) — out of scope; this is a local in-process viewer
- Mobile / MAUI / Blazor targets — WPF-only by design; no cross-platform UI planned
- OpenTelemetry integration — out of scope for v1.0; may be considered post-v1.0
- Structured logging properties (beyond flat string messages) — out of scope for v1.0
- Non-Windows .NET targets — `System.Windows.Threading.Dispatcher` is fundamentally Windows-only

## Context

**Architecture Review (2026-05-16):** A full review scored the library at 67/100 (Grade D), primarily penalised by the TFM mismatch. The underlying code quality — thread safety, WPF performance settings, documentation — is genuinely above average. See `.Analysis/ArchitectureReview_20260516_120000.md` for the full report.

**Dual integration patterns:** The library currently supports two integration paths (DI via `ILoggerProvider` and direct inheritance via `BaseLogger`). These share the same `BaseLoggerSink.Instance` singleton and global statics on `BaseLogger.Settings.cs`. Phase 3 replaces the global statics with `IOptions<LogViewerOptions>` and marks the inheritance pattern obsolete.

**Versioning:** All breaking changes (Color removal, global state restructure, inheritance deprecation) are bundled into a single v1.0.0 release. The project is in a pre-release state, so semver major-version discipline starts from v1.0.0.

**Test coverage:** `LogControlViewModel` instance-level tests are explicitly deferred to Phase 2/3 (tracked in the existing test file) because the `Dispatcher` constructor parameter blocks non-WPF test runners. `IDispatcher` abstraction in Phase 2 unlocks these tests.

## Constraints

- **Tech Stack**: WPF, .NET 8 + .NET 10 Windows, `Microsoft.Extensions.Logging` abstraction
- **Compatibility**: Public NuGet — breaking changes only in semver majors; v1.0.0 is the break point
- **Platform**: Windows-only (`net*-windows` TFMs); `System.Windows.Threading.Dispatcher` dependency is unavoidable for WPF UI marshalling
- **Dependencies**: Keep external dependencies minimal — `CommunityToolkit.Mvvm`, `Fody`/`PropertyChanged.Fody`, `CsvHelper` are justified; `Newtonsoft.Json` to be replaced by BCL `System.Text.Json`
- **CI**: GitHub Actions with `nuget-publish.yml`; must build and test both TFMs before publishing

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| Multi-target `net8.0-windows;net10.0-windows` (not downgrade to net8) | Keeps .NET 10 API access for future improvements; enables .NET 8 consumers immediately | — Pending |
| Single v1.0.0 bundles all breaking changes | Avoids repeated semver major bumps on a pre-release library; gives consumers one clean migration | — Pending |
| Replace `Newtonsoft.Json` with `System.Text.Json` | Reduces consumer dependency footprint; BCL, no extra package | — Pending |
| `IDispatcher` abstraction over raw `Dispatcher` | Unlocks unit testing of `LogControlViewModel` without WPF runtime | — Pending |
| Mark inheritance pattern `[Obsolete]` in v1.0 | DI pattern is strictly superior; inheritance pattern creates global state conflicts | — Pending |
| `LogColor` struct replaces `System.Windows.Media.Color` in core interfaces | Decouples logging abstractions from WPF; enables headless testing | — Pending |

## Evolution

This document evolves at phase transitions and milestone boundaries.

**After each phase transition** (via `/gsd-transition`):
1. Requirements invalidated? → Move to Out of Scope with reason
2. Requirements validated? → Move to Validated with phase reference
3. New requirements emerged? → Add to Active
4. Decisions to log? → Add to Key Decisions
5. "What This Is" still accurate? → Update if drifted

**After each milestone** (via `/gsd-complete-milestone`):
1. Full review of all sections
2. Core Value check — still the right priority?
3. Audit Out of Scope — reasons still valid?
4. Update Context with current state

---
*Last updated: 2026-05-16 after initialization*
