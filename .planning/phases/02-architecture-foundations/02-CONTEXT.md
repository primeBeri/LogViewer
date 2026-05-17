# Phase 2: Architecture Foundations - Context

**Gathered:** 2026-05-17
**Status:** Ready for planning
**Mode:** Auto-generated (infrastructure phase — all success criteria are purely technical)

<domain>
## Phase Boundary

Decouple WPF platform types from core interfaces, introduce an `IDispatcher` abstraction so `LogControlViewModel` is testable without a WPF runtime, and cap the pause buffer to prevent unbounded memory growth.

Specifically:
- Replace `System.Windows.Media.Color` with a platform-neutral `LogColor` struct in `ILoggable`, `LogEventArgs`, and `BaseLogger`
- Add a WPF-specific `LogColor.ToSolidColorBrush()` extension method; update XAML converters to use it
- Introduce `IDispatcher` interface (`CheckAccess`, `Invoke`, `InvokeAsync`) + `WpfDispatcher` concrete implementation
- Update `LogControlViewModel` constructor to accept `IDispatcher` instead of `Dispatcher`
- Update `LogControl.xaml.cs` to instantiate `WpfDispatcher` and pass it to the ViewModel
- Cap `_pauseBuffer` at `MaxLogSize` entries when `IsPaused` is `true` (BUG-02)

</domain>

<decisions>
## Implementation Decisions

### Claude's Discretion
All implementation choices are at Claude's discretion — pure infrastructure phase. Use ROADMAP phase goal, success criteria, and codebase conventions to guide decisions.

Key constraints from REQUIREMENTS.md:
- `LogColor` must be a value type (`struct`) with `byte A, R, G, B` fields
- `IDispatcher` must expose at minimum: `CheckAccess()`, `Invoke()`, `InvokeAsync()`
- `WpfDispatcher` wraps `System.Windows.Threading.Dispatcher`
- Pause buffer cap: when `IsPaused`, new entries beyond `MaxLogSize` should be dropped (not added)
- WPF rendering must be unchanged — `ColorToBrushConverter` and `LogLevelColorConverter` must still work

</decisions>

<code_context>
## Existing Code Insights

### Reusable Assets
- `LogControlViewModel._pauseBuffer` — `List<LogEventArgs>` protected by `_pauseLock`; `OnLogEventAsync` adds without cap
- `LogControlViewModel.DispatchIfNecessary / DispatchIfNecessaryAsync` — three overloads already exist; all delegate to `_dispatcher` field (type `Dispatcher`)
- `ColorToBrushConverter` — converts `System.Windows.Media.Color` → `SolidColorBrush`; needs updating to `LogColor`
- `LogLevelColorConverter` — uses `System.Windows.Media.Colors.*` static values; will need `LogColor` equivalents
- `LogEventArgs` constructor: `(LogLevel level, string logHandle, string message, Color color)` — the `color` param type must change to `LogColor`
- `BaseLogger` both constructors set `LogColor = color ?? Colors.Black` / `LogColor = color` — type must change
- `ILoggable.LogColor` property currently typed as `System.Windows.Media.Color`

### Established Patterns
- XML doc comments required on all public members
- `ArgumentNullException.ThrowIfNull` at public API boundaries
- `WPF Dispatcher` usage always goes through `DispatchIfNecessaryAsync` in ViewModel
- Partial class pattern used in `BaseLogger` (`BaseLogger.cs` + `BaseLogger.Settings.cs`)
- Value types (structs) used for lightweight data transfer

### Integration Points
- `LogControl.xaml.cs` creates `LogControlViewModel` — will need to pass `WpfDispatcher`
- `LogEventArgs.LogColor.ToString()` used in `FormatLogMessage` — `LogColor` struct needs a `ToString()` override
- `LogEventArgs.FormatLogMessage` references `{color}` placeholder — `LogColor.ToString()` output should match current `Color.ToString()` format or be documented

</code_context>

<specifics>
## Specific Ideas

- `LogColor` struct should live in `LogViewer` namespace (same assembly, no new project needed)
- Extension class `LogColorExtensions` in `LogViewer` namespace (or a sub-namespace) for `ToSolidColorBrush()`; keep WPF extension in the WPF assembly since it references `System.Windows.Media`
- `IDispatcher` interface in `LogViewer` namespace
- `WpfDispatcher` concrete class in `LogViewer` namespace
- Pause buffer cap: in `OnLogEventAsync`, when adding to `_pauseBuffer`, check `_pauseBuffer.Count >= MaxLogSize` under the lock and drop (return without adding) if at capacity

</specifics>

<deferred>
## Deferred Ideas

None — infrastructure phase, no scope creep possible.

</deferred>
