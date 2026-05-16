# Architecture

**Analysis Date:** 2026-05-16

## System Overview

```text
┌─────────────────────────────────────────────────────────────────────────────┐
│                           Consumer Application                               │
│  (Subclass of BaseLogger  OR  ILogger<T> via DI)                            │
│  `LogViewerExample/ExampleVM.cs`  `LogViewerExample/SomeObject.cs`          │
└───────────────────────┬───────────────────┬─────────────────────────────────┘
                        │ ILogger.Log()     │ BaseLogger.Log*()
                        ▼                   ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                         BaseLogger / Logger                                  │
│     `LogViewer/BaseLogger.cs`  `LogViewer/Logger.cs`                        │
│   Implements ILogger + ILoggable.  Wraps inner ILogger (pass-through),      │
│   builds LogEventArgs, fires LogEvent async event, writes to sink.          │
└───────────────────────────────────┬─────────────────────────────────────────┘
                                    │ IBaseLoggerSink.Write(LogEventArgs)
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                           BaseLoggerSink (singleton)                         │
│     `LogViewer/BaseLoggerSink.cs`                                           │
│   Thread-safe. Maintains ConcurrentQueue<LogEventArgs> (replay buffer).     │
│   Fires LogReceived event to all subscribers.  Trims oldest when            │
│   queue exceeds MaxQueueSize.                                               │
└───────────────────────────────────┬─────────────────────────────────────────┘
                                    │ LogReceived event (async)
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                         LogControlViewModel                                  │
│     `LogViewer/LogControlViewModel.cs`                                      │
│   Subscribes to sink. Filters by handle (regex) and log level.              │
│   Manages pause buffer. Dispatches to UI thread. Owns LogCollection.        │
└───────────────────────────────────┬─────────────────────────────────────────┘
                                    │ ReadOnlyLogCollection (INotifyCollectionChanged)
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                    LogControl (WPF UserControl)                              │
│     `LogViewer/LogControl.xaml` + `LogViewer/LogControl.xaml.cs`            │
│   Data-bound ListView. Virtualized rendering. Dynamically generated         │
│   DataTemplate for configurable display format. Export / Pause / Clear.     │
└─────────────────────────────────────────────────────────────────────────────┘
```

## Component Responsibilities

| Component | Responsibility | File |
|-----------|----------------|------|
| `BaseLogger` | Core logging implementation. Implements `ILogger` and `ILoggable`. Owns static global settings. Two construction modes: inheritance and DI. | `LogViewer/BaseLogger.cs`, `LogViewer/BaseLogger.Settings.cs` |
| `Logger` | Sealed concrete subclass of `BaseLogger` used by `BaseLogger.CreateLogger()` in the inheritance pattern. | `LogViewer/Logger.cs` |
| `BaseLoggerProvider` | `ILoggerProvider` for the DI pattern. Creates and caches `BaseLogger` instances per category via `ConcurrentDictionary<string, Lazy<BaseLogger>>`. | `LogViewer/BaseLoggerProvider.cs` |
| `BaseLoggerSink` | Thread-safe singleton. Central relay between loggers and the UI. Holds the replay queue. Fires `LogReceived` async event. | `LogViewer/BaseLoggerSink.cs` |
| `IBaseLoggerSink` | Abstraction for the sink, enabling test injection. | `LogViewer/IBaseLoggerSink.cs` |
| `LogControlViewModel` | MVVM ViewModel. Consumes `LogReceived`, applies filtering, manages pause buffer, exposes `ReadOnlyLogCollection` for binding. | `LogViewer/LogControlViewModel.cs` |
| `LogControl` | WPF `UserControl`. Data-bound to `LogControlViewModel`. Exposes WPF Dependency Properties. Generates `DataTemplate` at runtime from format string. | `LogViewer/LogControl.xaml`, `LogViewer/LogControl.xaml.cs` |
| `LogWindow` | Thin WPF `Window` wrapper containing a `LogControl` for standalone window use. | `LogViewer/LogWindow.xaml` |
| `LogCollection` | Thread-safe observable collection with duplicate prevention (`HashSet` + `List`). Internal to the VM layer. | `LogViewer/LogCollection.cs` |
| `ReadOnlyLogCollection` | Read-only public view over `LogCollection`. Forwards `INotifyCollectionChanged` for WPF binding. | `LogViewer/ReadOnlyLogCollection.cs` |
| `LogEventArgs` | Immutable data record for a single log entry: level, handle, message, color, timestamp, thread ID. | `LogViewer/LogEventArgs.cs` |
| `LogExporter` | Pure-function static class: serializes `LogEventArgs` collections to JSON, CSV, or plain-text. No file I/O. | `LogViewer/LogExporter.cs` |
| `LogEventArgsMap` | CsvHelper `ClassMap` defining the CSV column layout for `LogEventArgs`. | `LogViewer/LogEventArgsMap.cs` |
| `BaseLoggerLoggingBuilderExtensions` | `ILoggingBuilder.AddLogViewer(...)` extension methods. Wires `BaseLoggerProvider` into the DI container. | `LogViewer/BaseLoggerLoggingBuilderExtensions.cs` |
| `BaseLoggerProviderOptions` | POCO configuration object for the DI setup: min level, max queue, category colors, datetime format, UTC flag. | `LogViewer/BaseLoggerProviderOptions.cs` |
| Value Converters | WPF `IValueConverter` implementations for data binding: `Color→Brush`, `LogLevel→Brush`, `bool→!bool`. | `LogViewer/Converters/` |

## Pattern Overview

**Overall:** MVVM (Model-View-ViewModel) with a Provider / Sink pipeline.

**Key Characteristics:**
- The library is a WPF control packaged as a NuGet. It plugs into `Microsoft.Extensions.Logging` as a custom `ILoggerProvider`.
- A singleton `BaseLoggerSink` decouples log producers from the UI; it is the single point through which all log events flow to the control.
- The ViewModel (`LogControlViewModel`) is the only subscriber to the sink's event; it manages all state for the UI.
- The public surface of `LogCollection` is protected by a `ReadOnlyLogCollection` wrapper so external callers cannot mutate the VM's internal state.
- All UI mutations are marshalled onto the WPF `Dispatcher` via `DispatchIfNecessaryAsync`.

## Layers

**Logging Infrastructure Layer:**
- Purpose: Integrate with `Microsoft.Extensions.Logging`, capture log calls, produce `LogEventArgs`.
- Location: `LogViewer/BaseLogger.cs`, `LogViewer/BaseLoggerProvider.cs`, `LogViewer/BaseLoggerLoggingBuilderExtensions.cs`
- Contains: `ILogger` implementation, `ILoggerProvider`, DI extension methods.
- Depends on: `IBaseLoggerSink`, `LogEventArgs`
- Used by: Consumer applications, the DI container.

**Sink / Message Bus Layer:**
- Purpose: Thread-safe relay of `LogEventArgs` from any thread to any number of async subscribers. Maintains a capped replay queue.
- Location: `LogViewer/BaseLoggerSink.cs`, `LogViewer/IBaseLoggerSink.cs`
- Contains: Singleton `BaseLoggerSink`, `ConcurrentQueue<LogEventArgs>`, `LogEvent` async event.
- Depends on: `LogEventArgs`, `Delegates.cs`
- Used by: `BaseLogger` (writes to it), `LogControlViewModel` (subscribes to it), test code.

**ViewModel Layer:**
- Purpose: Business logic for the log viewer UI — filtering, pausing, trimming, exporting, dispatcher marshalling.
- Location: `LogViewer/LogControlViewModel.cs`
- Contains: Filter state (regex + level), pause buffer, `LogCollection`, `IAsyncRelayCommand` bindings.
- Depends on: `IBaseLoggerSink`, `LogCollection`, `ReadOnlyLogCollection`, `LogExporter`
- Used by: `LogControl` (code-behind creates it).

**View Layer:**
- Purpose: WPF rendering, user interaction, Dependency Properties.
- Location: `LogViewer/LogControl.xaml`, `LogViewer/LogControl.xaml.cs`, `LogViewer/LogWindow.xaml`
- Contains: `ListView` with virtualized rendering, toolbar (filter, level selector, export, pause, clear), dynamically generated `DataTemplate`.
- Depends on: `LogControlViewModel`, `ReadOnlyLogCollection`, `Converters/`
- Used by: Host WPF application.

**Data / Model Layer:**
- Purpose: Immutable log event record, observable collection, export serialization.
- Location: `LogViewer/LogEventArgs.cs`, `LogViewer/LogCollection.cs`, `LogViewer/ReadOnlyLogCollection.cs`, `LogViewer/LogExporter.cs`, `LogViewer/LogEventArgsMap.cs`
- Depends on: `Newtonsoft.Json`, `CsvHelper`
- Used by: All layers.

## Data Flow

### DI Pattern — Log Call to UI

1. Consumer calls `ILogger<T>.LogInformation(...)` on a DI-injected logger (`LogViewerExample/ExampleVM.cs`).
2. The `ILogger` is a `BaseLogger` created by `BaseLoggerProvider.CreateLogger()` (`LogViewer/BaseLoggerProvider.cs:52`).
3. `BaseLogger.Log(LogLevel, string)` builds a `LogEventArgs` and calls `_sink.Write(args)` (`LogViewer/BaseLogger.cs:179–200`).
4. `BaseLoggerSink.Write()` enqueues the event into `ConcurrentQueue<LogEventArgs>`, increments the atomic counter, and calls `RaiseLogReceivedAsync()` (`LogViewer/BaseLoggerSink.cs:49–67`).
5. `LogControlViewModel.OnLogEventAsync()` is invoked via the `LogReceived` event; it applies handle-regex and level filters, then either buffers (if paused) or dispatches to the UI thread (`LogViewer/LogControlViewModel.cs:315–332`).
6. `AddAndTrimLogEventsIfNeededAsync()` adds the event to `LogCollection` on the dispatcher thread and trims oldest entries if `MaxLogSize` is exceeded (`LogViewer/LogControlViewModel.cs:531–560`).
7. `ReadOnlyLogCollection` fires `CollectionChanged`; the `ListView` in `LogControl` re-renders the new item; `HandleCollectionChanged` auto-scrolls if enabled (`LogViewer/LogControl.xaml.cs:319–335`).

### Inheritance Pattern — Log Call to UI

1. Consumer class extends `BaseLogger` and calls `BaseLogger.Initialize(loggerFactory)` at startup.
2. `this.LogInformation("...")` calls `Log(LogLevel, string)` on the `BaseLogger` base.
3. Flow continues from step 3 above. The `Logger` field is a pass-through to the `ILoggerFactory`-created `ILogger`; `_sink` is not set in this path so `BaseLoggerSink.Instance` is used implicitly via the static singleton.
4. The `LogEvent` instance event is also fired, allowing per-instance subscribers in addition to the sink.

### Filter-Change Re-Render Flow

1. User changes handle filter text or log level in `LogControl` toolbar.
2. VM property setter calls `UpdateVisibleLogsAsync()` (`LogViewer/LogControlViewModel.cs:457`).
3. The full `sink.LogQueue` is scanned in a single pass, filtered, sorted by timestamp, trimmed to `MaxLogSize`, and bulk-added to `LogCollection` via `AddRange`.

### Export Flow

1. User clicks Export button → `ExportLogsCommand` fires → `LogControlViewModel.ExportLogsAsync()` (`LogViewer/LogControlViewModel.cs:668`).
2. `GetLogExportFilePathWithName()` shows `SaveFileDialog` on the UI thread.
3. A snapshot of `_logEvents` is taken and passed to `LogExporter.GetLogsAsJsonTextAsync/GetLogsAsTextAsync/GetLogsAsCSVTextAsync` (`LogViewer/LogExporter.cs`).
4. Result is written to file via `StreamWriter`.

**State Management:**
- Global settings (log level, datetime format, max queue size, etc.) are held as `static` properties on `BaseLogger` (`LogViewer/BaseLogger.Settings.cs`). This is a single shared configuration for the process.
- Per-VM state (filter, pause, visible events) lives in `LogControlViewModel` instance fields. Multiple `LogControl` instances are possible, each with an independent VM.
- The sink's `ConcurrentQueue` serves as the persistent replay buffer; a new `LogControlViewModel` hydrates itself from it on construction.

## Key Abstractions

**`IBaseLoggerSink`:**
- Purpose: Decouples `BaseLogger` (producer) from `LogControlViewModel` (consumer). Enables test doubles.
- Examples: `LogViewer/BaseLoggerSink.cs` (production singleton), `LogViewer.Tests/TestBaseLoggerSink.cs` (test stub).
- Pattern: Event-driven sink with replay queue.

**`ILoggable`:**
- Purpose: Extended logger contract adding color, handle, typed collection overloads, and `LogException`.
- Examples: `LogViewer/ILoggable.cs`
- Pattern: Interface segregation over raw `ILogger`.

**`LogEvent` delegate:**
- Purpose: `async Task` delegate used for all log event subscriptions, ensuring non-blocking async propagation.
- File: `LogViewer/Delegates.cs`
- Pattern: `delegate Task LogEvent(object sender, LogEventArgs eventArgs)`

**`LogCollection` / `ReadOnlyLogCollection`:**
- Purpose: Observable, duplicate-safe collection with configurable batch notification modes.
- Pattern: Internal mutable `LogCollection` hidden behind a read-only public facade (`ReadOnlyLogCollection`).

## Entry Points

**DI Integration:**
- Location: `LogViewer/BaseLoggerLoggingBuilderExtensions.cs`
- Triggers: `services.AddLogging(b => b.AddLogViewer(...))`
- Responsibilities: Configures `BaseLoggerSink`, creates `BaseLoggerProvider`, registers both in the DI container.

**Inheritance Integration:**
- Location: `LogViewer/BaseLogger.Settings.cs` → `BaseLogger.Initialize(loggerFactory)`
- Triggers: Called once at application startup before any `BaseLogger` subclass is instantiated.
- Responsibilities: Sets static `LoggerFactory` and global settings.

**WPF Control Placement:**
- Location: `LogViewer/LogControl.xaml.cs` constructor
- Triggers: XAML instantiation of `<local:LogControl />`
- Responsibilities: Creates `LogControlViewModel`, sets `DataContext`, registers event handlers.

## Architectural Constraints

- **Threading:** Log writes (`BaseLoggerSink.Write`) happen on the caller's thread (any thread). UI mutations are marshalled to the WPF `Dispatcher` thread by `LogControlViewModel.DispatchIfNecessaryAsync`. `LogCollection` uses a `lock` object for its internal `List<T>` + `HashSet<T>`, but change notifications are fired outside the lock.
- **Global state:** `BaseLogger` holds numerous `static` properties (`LoggerFactory`, `MaxLogQueueSize`, `LogDateTimeFormat`, `LogExportFormat`, etc.) in `LogViewer/BaseLogger.Settings.cs`. `BaseLoggerSink` is a `Lazy<T>` singleton. This means only one logging configuration can be active per process.
- **Circular imports:** None detected; the dependency direction is strictly Logger → Sink → ViewModel → View.
- **Dual-write prevention:** `BaseLogger.Log()` checks `if (Logger is not BaseLogger)` before writing to `_sink` to prevent double-writes when `BaseLogger` instances wrap other `BaseLogger` instances in the DI chain (`LogViewer/BaseLogger.cs:188–191`).

## Anti-Patterns

### Avoid instantiating `BaseLoggerSink` directly

**What happens:** `BaseLoggerSink` has a `private` constructor; callers obtain it via `BaseLoggerSink.Instance`. A `CreateForTesting()` factory bypasses the singleton for tests.
**Why it's wrong:** Bypassing the singleton in production would create a disconnected sink that the `LogControl` never subscribes to.
**Do this instead:** Always use `BaseLoggerSink.Instance` in production code; use `BaseLoggerSink.CreateForTesting()` in tests.

### Do not call `BaseLogger.Initialize()` with the DI pattern

**What happens:** The example app calls both `builder.AddLogViewer()` (DI) and `BaseLogger.Initialize(loggerFactory)` (inheritance). Calling `Initialize()` on its own without DI would bypass `BaseLoggerProvider` entirely.
**Why it's wrong:** `Initialize()` sets global static state; calling it after `AddLogViewer()` can overwrite settings configured by `AddLogViewerCore`.
**Do this instead:** Use `AddLogViewer()` exclusively for DI-based apps. Only call `Initialize()` if your codebase contains classes that directly extend `BaseLogger`.

## Error Handling

**Strategy:** Defensive — exceptions inside log-event handlers or VM operations are caught and re-logged internally rather than propagated to the caller.

**Patterns:**
- `BaseLogger.OnRaiseLogEventAsync`: Catches per-handler exceptions and wraps them in `Task.FromException`; `Task.WhenAll` surfaces aggregate failures that are caught and logged via `LogErrorException` (`LogViewer/BaseLogger.cs:438–469`).
- `LogControlViewModel` methods: Each async operation (`AddAndTrimLogEventsIfNeededAsync`, `UpdateVisibleLogsAsync`, `ExportLogsAsync`) wraps its body in `try/catch` and logs to `_logger` or falls back to `Debug.WriteLine` if the logger is unavailable.
- `BaseLoggerSink.RaiseLogReceivedAsync`: Swallows handler exceptions with `Debug.WriteLine` to protect the sink's event loop.

## Cross-Cutting Concerns

**Logging:** `LogControlViewModel` uses `ILogger<LogControlViewModel>` for its own internal diagnostic messages. It resolves the factory from `BaseLoggerSink.Instance.LoggerFactory` (DI path) or `BaseLogger.LoggerFactory` (inheritance path).
**Validation:** Null-checks via `ArgumentNullException.ThrowIfNull` at every public API boundary. Regex filter validation in `LogControlViewModel.SetRegexFilterIfValid` catches `ArgumentException` and `NotSupportedException` silently, leaving the previous filter unchanged.
**Property Change Notification:** `PropertyChanged.Fody` weave (`FodyWeavers.xml`) auto-implements `INotifyPropertyChanged` on `LogControlViewModel` and `ExampleVM`, eliminating boilerplate.

---

*Architecture analysis: 2026-05-16*
