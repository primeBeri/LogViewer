# Coding Conventions

**Analysis Date:** 2026-05-16

## Naming Patterns

### Types
- Classes use `PascalCase`: `BaseLogger`, `LogControlViewModel`, `BaseLoggerSink`, `LogCollection`
- Interfaces use `I` prefix + `PascalCase`: `ILoggable`, `IBaseLoggerSink`
- Enums use `PascalCase` for both type and values: `BatchNotificationMode.Reset`, `BatchNotificationMode.Atomic`
- Delegates use `PascalCase` and are named after what they represent, not how they're used: `LogEvent` (not `LogEventHandler`)
- Sealed internal implementation classes that concretise a public base use the plain noun form: `Logger` (sealed, internal) extends `BaseLogger`

### Methods
- Public methods use `PascalCase`: `AddRange`, `RemoveRange`, `SanitizeHandle`, `CreateLogger`
- Private/internal helpers use `PascalCase`: `TrimQueueIfNeeded`, `RaiseLogReceivedAsync`, `OnLogEvent`
- Async methods are suffixed `Async`: `ExportLogsAsync`, `ClearLogsAsync`, `UpdateVisibleLogsAsync`, `OnLogEventAsync`
- Static factory methods on the main class follow the `CreateX` pattern: `BaseLogger.CreateLogger(...)`, `BaseLoggerSink.CreateForTesting()`

### Properties
- Public properties use `PascalCase` regardless of accessibility of backing field
- Private backing fields use `_camelCase` (underscore prefix): `_sink`, `_logEvents`, `_pauseBuffer`, `_excludeCharsSet`
- Static backing fields also use `_camelCase`: `_logExportFormat`, `_defaultLogDisplayFormat`, `_excludeCharsLock`
- Static readonly dictionaries/arrays of log actions are `PascalCase`: `LogActions`, `LogExceptionActions`, `LogTraceMessage`

### Files
- One primary type per file, named after the type: `BaseLogger.cs`, `LogCollection.cs`
- Partial class extensions use `TypeName.Purpose.cs`: `BaseLogger.Settings.cs` (static settings/configuration partial)
- XAML codebehind follows WPF convention: `LogControl.xaml` + `LogControl.xaml.cs`
- Converters in a `Converters/` subdirectory: `ColorToBrushConverter.cs`, `LogLevelColorConverter.cs`

### Constants and Static Readonly
- Internal string format constants use `PascalCase` with `Default` prefix where applicable: `DefaultLogDateTimeFormat`, `DefaultLogExportFormat`, `LogDisplayFormatFallback`
- XAML resource key constants are named with a `Key` suffix and use `nameof(...)` for the value: `ColorToBrushConverterKey = nameof(ColorToBrushConverterKey)`

---

## Code Style Patterns

### Nullable Reference Types
- Enabled project-wide (`<Nullable>enable</Nullable>` in `LogViewer.csproj`)
- Nullable returns expressed with `?`: `ILogger?`, `LogEvent?`, `string?`, `IDisposable?`
- Null-coalescing assignment (`??=`) used for lazy default population: `ExcludeCharsFromHandle ??= [];`
- Null-conditional operators used throughout: `_sink?.Write(args)`, `Logger?.BeginScope(state)`
- Null-forgiving operator (`!`) used only in tests for null-argument guard tests: `c.Add(null!)`
- Properties with null guards validate in the initializer using null-coalescing throw:
  ```csharp
  public string LogHandle { get; } = logHandle ?? throw new ArgumentNullException(nameof(logHandle));
  ```

### Expression-Bodied Members
- Used extensively for single-expression methods (delegates to overloads):
  ```csharp
  public void LogCritical(string message) => Log(LogLevel.Critical, message);
  public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel;
  ```
- Used for simple property getters:
  ```csharp
  public static IReadOnlyList<FileType> SupportedExportFileTypes => BaseLogger.SupportedExportFileTypes;
  ```
- Not used when the body requires any branching or multi-statement work

### Switch Expressions
- Preferred over `switch` statements for value-returning branches:
  ```csharp
  StringBuilder contents = (SelectedExportFileType?.Extension ?? ".json") switch
  {
      ".json" => await LogExporter.GetLogsAsJsonTextAsync(exportLogs),
      ".txt"  => await LogExporter.GetLogsAsTextAsync(exportLogs, LogExportFormat),
      ".csv"  => await LogExporter.GetLogsAsCSVTextAsync(exportLogs),
      _       => new StringBuilder()
  };
  ```
- Also used in `LogLevelColorConverter` and `LogEventArgs.FormatLogMessage`

### Collection Initialization
- C# 12 collection expression syntax `[...]` used for arrays and lists:
  ```csharp
  private readonly List<LogEventArgs> _pauseBuffer = [];
  private readonly LogCollection _logEvents = [];
  IReadOnlyCollection<char> _excludeCharsFromHandle = ['.', '-', ' '];
  ```
- Dictionary initializers use `new()` with collection initializer syntax: `new() { { key, value } }`

### Partial Classes
- `BaseLogger` is split across two files using `partial`:
  - `BaseLogger.cs` — instance state, constructors, instance methods
  - `BaseLogger.Settings.cs` — all static state, static methods, static constants, factory methods
- `LogControl.xaml.cs` is a standard WPF partial class (codebehind)

### Primary Constructors (C# 12)
- Used on `BaseLoggerProvider`:
  ```csharp
  public sealed class BaseLoggerProvider(IBaseLoggerSink sink, ILoggerFactory? innerFactory = null) : ILoggerProvider
  ```
- Used on `Logger` (sealed internal subclass):
  ```csharp
  internal sealed class Logger(string? handle = null, Color? color = null, LogLevel logLevel = LogLevel.Information) : BaseLogger(...)
  ```
- Used on `LogEventArgs` (primary constructor, with null-guard in property initializers):
  ```csharp
  public class LogEventArgs(LogLevel level, string logHandle, string message, Color color) : EventArgs
  ```

### `sealed` Usage
- Applied aggressively to types not designed for inheritance: `BaseLoggerSink`, `BaseLoggerProvider`, `ReadOnlyLogCollection`, `LogEventArgsMap`, `Logger`, `ExportLogResult`, `FileType`, `GlobalStateCollection`

### Discard Operator
- Fire-and-forget async calls use `_ =` to suppress compiler warnings:
  ```csharp
  _ = OnRaiseLogEventAsync(LogEvent, eventArgs);
  _ = UpdateVisibleLogsAsync();
  ```

### `IDisposable` Implementation
- Full Dispose pattern with `protected virtual Dispose(bool disposing)` and public `Dispose()` calling `GC.SuppressFinalize(this)`, used in `LogControlViewModel` and `LogControl`

---

## Error Handling Patterns

### Guard Clauses at Method Entry
- `ArgumentNullException.ThrowIfNull(x)` is the standard for reference parameters at public/internal API boundaries:
  ```csharp
  ArgumentNullException.ThrowIfNull(logEvents, paramName: nameof(logEvents));
  ArgumentNullException.ThrowIfNull(sink);
  ```
- Primary constructor field initializers use inline null-coalescing throw for immutable state:
  ```csharp
  private readonly IBaseLoggerSink _sink = sink ?? throw new ArgumentNullException(nameof(sink));
  ```
- Inline guard in constructors for mutable context:
  ```csharp
  _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
  ```

### Null Checks for Optional Inputs (silent return)
- Methods that accept nullable inputs check and silently return rather than throw when nil data is valid:
  ```csharp
  public void Log(LogLevel level, string message) { if (message is null) return; ... }
  public void Write(LogEventArgs logEvent) { if (logEvent is null) return; ... }
  ```

### Exception Swallowing in Event Dispatch
- Exceptions thrown inside async event handlers are caught per-handler and either re-wrapped (`Task.FromException`) or written to `Debug.WriteLine` to avoid crashing the logging pipeline:
  ```csharp
  catch (Exception ex) { return Task.FromException(ex); }
  ```
- Top-level `Task.WhenAll` exceptions in `OnRaiseLogEventAsync` are caught and logged via `LogErrorException`.

### Catch-When Filtering
- Used in `SetRegexFilterIfValid` to handle expected failure modes without catching unrelated exceptions:
  ```csharp
  catch (Exception ex) when (ex is ArgumentException or NotSupportedException)
  ```

### Logging Errors Internally
- When `ILogger` is available, errors are recorded with `BaseLogger.LogErrorException(_logger, message, ex)`.
- When no logger is available (design mode, pre-initialization), errors fall back to `Debug.WriteLine(message)` + `Debug.WriteLine(ex)`.

### Exceptions in Collection Operations
- `InvalidOperationException` thrown for semantic violations (duplicate insert, item-not-found on removal)
- `ArgumentOutOfRangeException` thrown for index/count bounds violations, with descriptive messages

---

## Logging Patterns

The library is itself a logging infrastructure component. Internal logging (errors within the library) follows a consistent pattern:

### How Internal Errors Are Reported
```csharp
// Preferred: when ILogger is initialized
BaseLogger.LogErrorException(_logger, "Error while clearing visible logs in LogControlViewModel.", ex);

// Fallback: when ILogger is unavailable (design mode / not yet initialized)
Debug.WriteLine(message);
Debug.WriteLine(ex);
```

### LoggerMessage.Define (Performance Pattern)
Static pre-compiled log actions are used for all internal pass-through logging, following Microsoft's `LoggerMessage.Define` high-performance pattern:
```csharp
internal static readonly Action<ILogger, string, Exception?> LogErrorMessage =
    LoggerMessage.Define<string>(LogLevel.Error, new EventId(4, nameof(LogError)), "{Message}");
```
Six message actions (EventId 0–5) and six exception actions (EventId 100–105) cover all `LogLevel` values and are looked up via dictionary dispatch.

### Sink-Based Architecture
All log output routes through `IBaseLoggerSink.Write(LogEventArgs)`. The `BaseLoggerSink` fires a `LogEvent` async event for UI subscribers. This means there is no direct logger call for the UI path — the event system is the "log transport."

---

## Dependency Injection Patterns

### IBaseLoggerSink Registered as Singleton
`AddLogViewer()` extension method registers `BaseLoggerSink.Instance` as `IBaseLoggerSink` via `TryAddSingleton`. Consumer VMs receive the sink via constructor injection.

### IBaseLoggerSink as a Seam for Testing
`LogControlViewModel` and `BaseLogger` (DI constructor) both accept `IBaseLoggerSink` as a constructor parameter with a default:
```csharp
public LogControlViewModel(Dispatcher dispatcher, IBaseLoggerSink? sink = null, string? logHandleFilter = null)
{
    _sink = sink ?? BaseLoggerSink.Instance;
    ...
}
```
This allows test code to substitute `TestBaseLoggerSink` without touching the DI container.

### ILoggerProvider Integration
`BaseLoggerProvider` implements `ILoggerProvider` and can be added directly to any `ILoggingBuilder`:
```csharp
builder.AddProvider(provider); // manual
// or
builder.AddLogViewer();        // via extension method
```

### Extension Method Configuration Object Pattern
DI configuration uses an options object (`BaseLoggerProviderOptions`) rather than ad-hoc parameters, configured via an `Action<BaseLoggerProviderOptions>` callback:
```csharp
builder.AddLogViewer(options =>
{
    options.MaxQueueSize = 5000;
    options.MinimumLevel = LogLevel.Warning;
    options.CategoryColors["MyService"] = Colors.Blue;
});
```

### Interface Abstractions
- `ILoggable` — public contract for the inheritance-pattern consumer API
- `IBaseLoggerSink` — abstracts the log event sink for both DI registration and test substitution
- `ILogger` (Microsoft.Extensions.Logging) — `BaseLogger` implements this directly for DI framework compatibility

---

## Analysis Rules and Enforcement

- `<EnforceCodeStyleInBuild>True</EnforceCodeStyleInBuild>` — code style rules are enforced at build time in the library project (`LogViewer.csproj`)
- `<AnalysisLevel>latest-recommended</AnalysisLevel>` — Roslyn analyzer rules from the latest recommended ruleset are applied
- No `.editorconfig`, `.ruleset`, or `.globalconfig` files are present — all rule configuration is in the project file
- The test project (`LogViewer.Tests.csproj`) does **not** set `EnforceCodeStyleInBuild`
- One targeted suppression exists in the codebase:
  ```csharp
  [System.Diagnostics.CodeAnalysis.SuppressMessage(
      "Roslynator", "RCS1229:Use async/await when necessary",
      Justification = "Awaiting all tasks after select statement, need to trigger all invokers without delay")]
  ```
  This is in `BaseLogger.OnRaiseLogEventAsync` and is justified inline
- `GenerateDocumentationFile` is `True` — all public/internal members must have XML documentation comments; this is enforced by the compiler warning CS1591

---

## XML Documentation

- All public API members carry `<summary>`, `<param>`, `<returns>`, and `<exception>` XML doc comments
- Internal members directly visible to the test project (via `InternalsVisibleTo`) also carry XML docs
- `<remarks>` blocks are used for longer explanations of non-obvious behaviour (e.g., dual-sink prevention, thread-safety caveats)
- `<inheritdoc/>` is used on interface implementations where the interface already documents the contract
- Format: each XML tag on its own line, no inline multi-tag compression
