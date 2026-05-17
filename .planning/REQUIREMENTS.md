# Requirements: LogViewer (RealTimeLogStream)

**Defined:** 2026-05-16
**Core Value:** A plug-and-play WPF log viewer that any .NET 8 or .NET 10 application can embed via NuGet with a single `AddLogViewer()` call and a `<LogControl />` element — no boilerplate, no friction.

---

## v1 Requirements

All requirements below target the v1.0.0 public NuGet release. Requirements are derived from the architecture review in `.Analysis/ArchitectureReview_20260516_120000.md` and the brownfield codebase state.

### Target Framework (TFM)

- [ ] **TFM-01**: The NuGet package targets both `net8.0-windows` and `net10.0-windows` so a consuming WPF application on either runtime can install the package
- [ ] **TFM-02**: `Microsoft.Extensions.*` package references are version-conditional — `8.0.x` when building for `net8.0-windows`, `10.0.x` when building for `net10.0-windows`
- [ ] **TFM-03**: README and NuGet package metadata accurately document supported frameworks (`net8.0-windows`, `net10.0-windows`)

### Bug Fixes (BUG)

- [ ] **BUG-01**: `ExportLogResult.Success` is set to `true` after a successful export; callers can rely on the return value to detect success vs failure
- [ ] **BUG-02**: `_pauseBuffer` in `LogControlViewModel` is capped at `MaxLogSize`; the buffer cannot grow unboundedly during a long pause under high log throughput

### Continuous Integration (CI)

- [ ] **CI-01**: The CI workflow (`nuget-publish.yml`) installs .NET SDK versions sufficient to build and test both `net8.0-windows` and `net10.0-windows` target frameworks
- [ ] **CI-02**: The CI pipeline publishes a NuGet package that contains both TFM output folders (`lib/net8.0-windows/` and `lib/net10.0-windows/`) and passes validation

### Architecture (ARCH)

- [ ] **ARCH-01**: `System.Windows.Media.Color` is removed from `ILoggable` and `LogEventArgs`; replaced with a platform-neutral `LogColor` value type (e.g., `struct LogColor { byte A, R, G, B }`)
- [ ] **ARCH-02**: A WPF-specific extension method (e.g., `LogColor.ToSolidColorBrush()`) converts `LogColor` to `SolidColorBrush` for use in XAML converters — no WPF reference in core interfaces
- [ ] **ARCH-03**: An `IDispatcher` interface abstracts `System.Windows.Threading.Dispatcher` in `LogControlViewModel` (`CheckAccess()`, `Invoke()`, `InvokeAsync()`)
- [ ] **ARCH-04**: A `WpfDispatcher` concrete class implements `IDispatcher` by wrapping the real `Dispatcher`; `LogControl` creates a `WpfDispatcher` and passes it to the ViewModel constructor

### Global State Elimination (STATE)

- [x] **STATE-01**: A `LogViewerOptions` POCO consolidates all shared configuration (`MaxLogQueueSize`, `LogDateTimeFormat`, `LogUTCTime`, `ExcludeCharsFromHandle`, category colour map) currently scattered across static properties on `BaseLogger.Settings.cs`
- [x] **STATE-02**: `LogViewerOptions` is registered as `IOptions<LogViewerOptions>` in the DI container via `AddLogViewer()` so consumers can configure it through `appsettings.json` or `Configure<LogViewerOptions>()`
- [x] **STATE-03**: `BaseLogger.Initialize()`, `BaseLogger.Shutdown()`, and the static inheritance-pattern factory methods are marked `[Obsolete("Use the DI pattern via builder.AddLogViewer(). See migration guide.")]`
- [x] **STATE-04**: All internal reads of static `BaseLogger.*` configuration properties in the DI code path are replaced by injected `IOptions<LogViewerOptions>`

### Testability (TEST)

- [x] **TEST-01**: `LogControlViewModel` instance-level tests exercise pause (`IsPaused`) toggle — events are buffered when paused, flushed when resumed
- [x] **TEST-02**: `LogControlViewModel` instance-level tests exercise handle filter and log-level filter updates including invalid regex handling
- [x] **TEST-03**: `LogControlViewModel` instance-level tests exercise `AddAndTrimLogEventsIfNeededAsync` — collection is trimmed to `MaxLogSize` when exceeded
- [x] **TEST-04**: `LogExporter` has dedicated unit tests verifying JSON, CSV, and plain-text output format for a known set of `LogEventArgs`

### Package / Dependencies (PKG)

- [x] **PKG-01**: `Newtonsoft.Json` is removed from `LogViewer.csproj` — no longer a transitive NuGet dependency for consumers
- [x] **PKG-02**: JSON export in `LogExporter` uses `System.Text.Json.JsonSerializer.Serialize` with appropriate `JsonSerializerOptions` (indented, enum strings)

### UI / UX (UI)

- [x] **UI-01**: `LogWindow` either hosts a functional `LogControl` (providing a standalone pop-out log window) or is removed from the package entirely — it must not ship as an empty `<Grid/>` shell
- [x] **UI-02**: `LogControl.xaml` background is no longer hardcoded as `White`; it uses `{DynamicResource {x:Static SystemColors.WindowBrushKey}}` (or an exposed `Background` dependency property) so the control inherits the host application's WPF theme

---

## v2 Requirements

Deferred to a post-v1.0 release. Tracked but not in the current roadmap.

### Observability

- **OBS-01**: Queue-depth metric and dropped-message counter exposed for consumer diagnostics
- **OBS-02**: `TaskScheduler.UnobservedTaskException` hook documented in README for Release-build diagnostics

### Performance

- **PERF-01**: `filteredLogs.Skip(startIndex).ToList()` in `UpdateVisibleLogsAsync` replaced with `filteredLogs.GetRange(startIndex, count)` to eliminate the extra LINQ allocation

### Structured Logging

- **STRUCT-01**: `LogEventArgs` captures structured logging properties (beyond flat message strings) for richer display and export

### OpenTelemetry

- **OTEL-01**: Optional OpenTelemetry integration as a separate companion package

---

## Out of Scope

Explicitly excluded for v1.0. Documented to prevent scope creep.

| Feature | Reason |
|---------|--------|
| Remote / network log streaming | Local in-process viewer by design; network adds transport complexity outside the library's scope |
| Mobile / MAUI / Blazor targets | WPF-only; `Dispatcher` and `System.Windows` are Windows-desktop-only |
| Non-Windows .NET targets | `net*-windows` TFMs are required; no headless server scenarios |
| OpenTelemetry integration | Complex; better suited as a companion package post-v1.0 |
| OAuth / authentication | No network, no auth surface |
| Real-time multi-process log aggregation | Out of scope; single-process embedded viewer |

---

## Traceability

All 23 v1 requirements mapped to phases. Confirmed during roadmap creation (2026-05-16).

| Requirement | Phase | Status |
|-------------|-------|--------|
| TFM-01 | Phase 1 | Pending |
| TFM-02 | Phase 1 | Pending |
| TFM-03 | Phase 1 | Pending |
| BUG-01 | Phase 1 | Pending |
| CI-01 | Phase 1 | Pending |
| CI-02 | Phase 1 | Pending |
| ARCH-01 | Phase 2 | Pending |
| ARCH-02 | Phase 2 | Pending |
| ARCH-03 | Phase 2 | Pending |
| ARCH-04 | Phase 2 | Pending |
| BUG-02 | Phase 2 | Pending |
| STATE-01 | Phase 3 | Complete |
| STATE-02 | Phase 3 | Complete |
| STATE-03 | Phase 3 | Complete |
| STATE-04 | Phase 3 | Complete |
| TEST-01 | Phase 3 | Complete |
| TEST-02 | Phase 3 | Complete |
| TEST-03 | Phase 3 | Complete |
| TEST-04 | Phase 3 | Complete |
| PKG-01 | Phase 4 | Complete |
| PKG-02 | Phase 4 | Complete |
| UI-01 | Phase 4 | Complete |
| UI-02 | Phase 4 | Complete |

**Coverage:**
- v1 requirements: 23 total
- Mapped to phases: 23
- Unmapped: 0 ✓

---
*Requirements defined: 2026-05-16*
*Last updated: 2026-05-17 after 03-01 execution — STATE-01 through STATE-04 marked complete*
