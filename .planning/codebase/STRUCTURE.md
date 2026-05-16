# Codebase Structure

**Analysis Date:** 2026-05-16

## Directory Layout

```
LogViewer/                             # Solution root
├── LogViewer/                         # Core library project (NuGet package)
│   ├── Converters/                    # WPF IValueConverter implementations
│   ├── Styles/                        # Shared WPF resource dictionaries
│   ├── AssemblyInfo.cs                # ThemeInfo attribute for WPF resource lookup
│   ├── BaseLogger.cs                  # Core ILogger + ILoggable implementation
│   ├── BaseLogger.Settings.cs         # Static global configuration (partial class)
│   ├── BaseLoggerLoggingBuilderExtensions.cs  # AddLogViewer() DI extension
│   ├── BaseLoggerProvider.cs          # ILoggerProvider for DI pattern
│   ├── BaseLoggerProviderOptions.cs   # Configuration POCO for AddLogViewer()
│   ├── BaseLoggerSink.cs              # Singleton thread-safe event relay
│   ├── BatchNotificationMode.cs       # Enum: Reset | Atomic | PerItem
│   ├── Delegates.cs                   # LogEvent async delegate definition
│   ├── ExportLogResult.cs             # Result record for export operations
│   ├── FileType.cs                    # Simple record (Name, Extension) for export
│   ├── FodyWeavers.xml                # Fody weaver config (PropertyChanged.Fody)
│   ├── IBaseLoggerSink.cs             # Sink abstraction interface
│   ├── ILoggable.cs                   # Extended logger interface
│   ├── LogCollection.cs               # Thread-safe observable collection
│   ├── LogControl.xaml                # WPF UserControl XAML
│   ├── LogControl.xaml.cs             # UserControl code-behind + DataTemplate generation
│   ├── LogControlViewModel.cs         # MVVM ViewModel for LogControl
│   ├── LogEventArgs.cs                # Immutable log event data record
│   ├── LogEventArgsMap.cs             # CsvHelper ClassMap for CSV export
│   ├── LogExporter.cs                 # Pure-function export helpers (JSON/CSV/TXT)
│   ├── Logger.cs                      # Sealed internal concrete BaseLogger subclass
│   ├── LogViewer.csproj               # Project file (net10.0-windows, UseWPF, NuGet)
│   ├── LogWindow.xaml                 # Standalone log Window (thin wrapper)
│   ├── LogWindow.xaml.cs              # Window code-behind
│   └── ReadOnlyLogCollection.cs       # Public read-only facade over LogCollection
│
├── LogViewer.Tests/                   # xUnit test project
│   ├── BaseLoggerLoggingBuilderExtensionsTests.cs
│   ├── BaseLoggerProviderTests.cs
│   ├── BaseLoggerSinkTests.cs
│   ├── BaseLoggerTests.cs
│   ├── GlobalStateCollection.cs       # xUnit collection fixture (serialises static state tests)
│   ├── IntegrationTests.cs
│   ├── LogCollectionTests.cs
│   ├── LogControlViewModelTests.cs
│   ├── LogEventArgsTests.cs
│   ├── LogExporterTests.cs
│   ├── LogViewer.Tests.csproj
│   └── TestBaseLoggerSink.cs          # Test double for IBaseLoggerSink
│
├── LogViewerExample/                  # WinExe sample application
│   ├── App.xaml / App.xaml.cs         # Application entry point; DI setup
│   ├── CustomCommand.cs               # ICommand wrapper for example UI buttons
│   ├── Delegates.cs                   # Example-local delegate(s)
│   ├── ExampleVM.cs                   # Example ViewModel using ILogger<T>
│   ├── FodyWeavers.xml
│   ├── LogViewerExample.csproj
│   ├── MainWindow.xaml / .cs          # Main application window
│   ├── nlog.config                    # NLog file-sink configuration
│   └── SomeObject.cs                  # Example class using BaseLogger inheritance
│
├── Resources/                         # NuGet package assets
│   └── console-network-outline.png    # Package icon
│
├── THIRD_PARTY_LICENSES/              # License texts for dependencies
│   ├── Apache-2.0.md
│   └── MIT.md
│
├── .Analysis/                         # Architecture review output (not shipped)
├── .claude/commands/                  # Claude Code custom commands
├── .github/workflows/                 # CI/CD (nuget-publish.yml)
├── .planning/codebase/                # GSD codebase map documents
├── LogViewer.sln                      # Visual Studio solution
├── nuget.config                       # NuGet source configuration
├── LICENSE.txt                        # Project license (included in package)
└── README.md                          # Usage documentation (included in package)
```

## Directory Purposes

**`LogViewer/` (library project):**
- Purpose: The entire distributable NuGet library. All public API lives here.
- Contains: WPF controls, ViewModel, logging infrastructure, sink, collection types, export helpers.
- Key files: `BaseLogger.cs`, `BaseLoggerSink.cs`, `LogControlViewModel.cs`, `LogControl.xaml`

**`LogViewer/Converters/`:**
- Purpose: WPF `IValueConverter` implementations used exclusively in `LogControl.xaml` data bindings.
- Contains: `ColorToBrushConverter.cs`, `InverseBooleanConverter.cs`, `LogLevelColorConverter.cs`

**`LogViewer/Styles/`:**
- Purpose: XAML resource dictionaries merged into `LogControl.xaml`.
- Contains: `ButtonStyles.xaml` (rounded button style used by Export / Pause / Clear buttons).

**`LogViewer.Tests/` (test project):**
- Purpose: xUnit unit and integration tests for the library.
- Contains: Per-class test files mirroring the library structure, plus `TestBaseLoggerSink.cs` (test double) and `GlobalStateCollection.cs` (prevents parallel test interference with `BaseLogger` static state).

**`LogViewerExample/` (WinExe):**
- Purpose: Runnable demonstration of both integration patterns (DI `ILogger<T>` and `BaseLogger` inheritance), NLog pass-through, and the `LogControl` UI.
- Contains: Application startup (`App.xaml.cs`), example ViewModel, main window, NLog config.

**`Resources/`:**
- Purpose: Static assets bundled into the NuGet package.
- Generated: No. Committed: Yes.

**`.github/workflows/`:**
- Purpose: GitHub Actions CI/CD pipeline that publishes the NuGet package on tag push.
- Key file: `nuget-publish.yml`

## Key File Locations

**Entry Points:**
- `LogViewerExample/App.xaml.cs`: Application startup. Configures NLog, builds DI container, calls `AddLogViewer()`, calls `BaseLogger.Initialize()`.
- `LogViewer/BaseLogger.Settings.cs` → `BaseLogger.Initialize()`: Inheritance-pattern initialization.
- `LogViewer/BaseLoggerLoggingBuilderExtensions.cs` → `AddLogViewer()`: DI-pattern initialization.

**Configuration:**
- `LogViewer/BaseLoggerProviderOptions.cs`: All configurable knobs exposed via `AddLogViewer(options => {...})`.
- `LogViewer/BaseLogger.Settings.cs`: Global static configuration fields (`MaxLogQueueSize`, `LogDateTimeFormat`, `LogExportFormat`, etc.).
- `LogViewerExample/nlog.config`: NLog XML configuration for the example app's file sink.
- `LogViewer/FodyWeavers.xml`: Enables `PropertyChanged.Fody` for the library.
- `LogViewerExample/FodyWeavers.xml`: Enables `PropertyChanged.Fody` for the example app.
- `nuget.config`: NuGet source configuration (nuget.org).

**Core Logic:**
- `LogViewer/BaseLogger.cs`: `ILogger.Log<TState>` implementation, `Log(LogLevel, string)`, event firing.
- `LogViewer/BaseLoggerSink.cs`: Singleton sink, queue management, async event broadcast.
- `LogViewer/LogControlViewModel.cs`: Filter logic, pause/resume, dispatcher marshalling, export orchestration.
- `LogViewer/LogExporter.cs`: Pure serialization logic for JSON/CSV/TXT.

**Testing:**
- `LogViewer.Tests/TestBaseLoggerSink.cs`: In-memory `IBaseLoggerSink` test double.
- `LogViewer.Tests/GlobalStateCollection.cs`: xUnit `[CollectionDefinition]` that serialises tests touching `BaseLogger` static state.
- `LogViewer.Tests/IntegrationTests.cs`: End-to-end tests from logger creation through sink to `LogEventArgs` verification.

## Naming Conventions

**Files:**
- Classes named after the component they implement: `BaseLoggerSink.cs`, `LogControlViewModel.cs`, `LogEventArgsMap.cs`.
- XAML paired files: `LogControl.xaml` + `LogControl.xaml.cs` (standard WPF partial class convention).
- Partial class split: `BaseLogger.cs` (methods) + `BaseLogger.Settings.cs` (static state and settings).
- Test files: `{ClassName}Tests.cs` mirroring the library class name.

**Directories:**
- PascalCase matching the project or functional area: `Converters/`, `Styles/`.

## Where to Add New Code

**New log formatter or display feature:**
- ViewModel state: `LogViewer/LogControlViewModel.cs`
- UI binding: `LogViewer/LogControl.xaml` (add Dependency Property in `LogControl.xaml.cs`)
- If it requires a new value converter: `LogViewer/Converters/`

**New export format:**
- Serialization method: `LogViewer/LogExporter.cs` (add a new `GetLogsAs*Async` method)
- Format registration: `LogViewer/BaseLogger.Settings.cs` → `SupportedExportFileTypes`
- Export dispatch: `LogViewer/LogControlViewModel.cs` → `ExportLogsAsync` switch statement

**New global configuration option:**
- Add property: `LogViewer/BaseLoggerProviderOptions.cs`
- Apply in DI setup: `LogViewer/BaseLoggerLoggingBuilderExtensions.cs` → `AddLogViewerCore`
- Optionally mirror as static property: `LogViewer/BaseLogger.Settings.cs`

**New `BaseLogger` method (e.g., new log level helper):**
- Implementation: `LogViewer/BaseLogger.cs`
- Interface contract: `LogViewer/ILoggable.cs`
- Unit tests: `LogViewer.Tests/BaseLoggerTests.cs`

**New unit test:**
- Mirror file naming: `LogViewer.Tests/{ClassName}Tests.cs`
- If the test reads or modifies `BaseLogger` static state, add `[Collection(GlobalStateCollection.Name)]` to the class and run serially.

## Special Directories

**`.planning/codebase/`:**
- Purpose: GSD codebase map documents (STACK.md, INTEGRATIONS.md, etc.).
- Generated: Yes (by GSD tooling). Committed: Yes.

**`.Analysis/`:**
- Purpose: Architecture review outputs generated by Claude commands.
- Generated: Yes. Committed: User decision (currently untracked per `.gitignore` status).

**`.claude/commands/`:**
- Purpose: Custom Claude Code slash commands for this repository.
- Generated: No. Committed: Yes.

**`bin/` and `obj/` (per-project):**
- Purpose: Build output and intermediate files.
- Generated: Yes. Committed: No (`.gitignore`).

---

*Structure analysis: 2026-05-16*
