# External Integrations

**Analysis Date:** 2026-05-16

## Overview

LogViewer (`RealTimeLogStream`) is a self-contained WPF control library. It has **no network calls, no cloud dependencies, no database, and no auth providers**. All integrations are with the .NET ecosystem's own logging and DI abstractions, plus a handful of NuGet packages for serialization. The `LogViewerExample` project adds NLog as a file-logging sink to demonstrate real-world usage alongside the library.

---

## Logging Ecosystem Integration

**Microsoft.Extensions.Logging (MEL):**
- Package: `Microsoft.Extensions.Logging` 10.0.6 + `Microsoft.Extensions.Logging.Abstractions` 10.0.6
- Integration point: `BaseLogger` implements `ILogger`; `BaseLoggerProvider` implements `ILoggerProvider`; `BaseLoggerLoggingBuilderExtensions` exposes `AddLogViewer(this ILoggingBuilder)` for registration
- How it connects: Consumers call `builder.AddLogViewer(...)` inside `services.AddLogging(...)` — standard MEL pattern; no special plumbing required
- Key files: `LogViewer/BaseLoggerLoggingBuilderExtensions.cs`, `LogViewer/BaseLoggerProvider.cs`, `LogViewer/BaseLogger.cs`

**NLog (example app only):**
- Packages: `NLog` 6.0.1, `NLog.Extensions.Logging` 6.0.1 (in `LogViewerExample/LogViewerExample.csproj` only)
- Integration point: `builder.AddNLog()` registered alongside `builder.AddLogViewer(...)` so log events flow to both the file and the UI control simultaneously
- Config file: `LogViewerExample/nlog.config` (XML) — writes to `Log\ALL.log`, archives at 10 MB, max 300 archive files
- Key file: `LogViewerExample/App.xaml.cs`

**ILoggerFactory wrapping:**
- `BaseLoggerProvider` accepts an optional `ILoggerFactory innerFactory` constructor argument, allowing LogViewer to delegate to any existing MEL-compatible factory (NLog, Serilog, etc.) while also capturing events in the UI sink
- Key file: `LogViewer/BaseLoggerProvider.cs`

---

## Dependency Injection Integration

**Microsoft.Extensions.DependencyInjection:**
- Abstractions package: `Microsoft.Extensions.DependencyInjection.Abstractions` 10.0.6 (library)
- Full container package: `Microsoft.Extensions.DependencyInjection` 10.0.6 (example app only)
- Integration: `BaseLoggerLoggingBuilderExtensions.AttachLoggerFactoryToLogViewer(this IServiceProvider)` is called post-`BuildServiceProvider()` to wire the resolved `ILoggerFactory` into `BaseLoggerSink.Instance`
- Key file: `LogViewer/BaseLoggerLoggingBuilderExtensions.cs`

---

## Serialization / Export

**Newtonsoft.Json:**
- Package: `Newtonsoft.Json` 13.0.3
- Used by: `LogExporter.GetLogsAsJsonTextAsync` — serializes `IEnumerable<LogEventArgs>` to indented JSON in-memory
- No network usage; output is written to a local file chosen via `SaveFileDialog`
- Key file: `LogViewer/LogExporter.cs`

**CsvHelper:**
- Package: `CsvHelper` 33.1.0
- Used by: `LogExporter.GetLogsAsCSVTextAsync` — writes log events as CSV using `LogEventArgsMap` class map
- No network usage; output is written to a local file
- Key files: `LogViewer/LogExporter.cs`, `LogViewer/LogEventArgsMap.cs`

---

## Data Storage

**In-memory only:**
- All log events are held in `BaseLoggerSink` (a singleton in-process queue, max size configurable, default 10,000 entries)
- `LogCollection` / `ReadOnlyLogCollection` provide the observable collection bound to the WPF UI
- No database, no file-based persistence of the live log queue

**File output (export / NLog):**
- Log export: user-triggered via `SaveFileDialog`; writes `.json`, `.csv`, or `.txt` to a path the user selects
- NLog (example only): writes to `Log\ALL.log` on the local filesystem

---

## Authentication & Identity

- None. The library has no auth requirements.
- The `nuget.config` at repo root references a GitHub Packages NuGet feed (`https://nuget.pkg.github.com/ArisenVendetta/index.json`) with a placeholder token (`GH_PKG_TOKEN`) for publishing the `RealTimeLogStream` package. This is a CI/publish concern only, not a runtime integration.

---

## Monitoring & Observability

**Error Tracking:**
- None (no Sentry, Application Insights, etc.)

**Internal logging:**
- `LogControlViewModel` and `BaseLogger` both log their own internal errors through the MEL pipeline using `BaseLogger.LogErrorException(...)` — errors surface in the same UI viewer

---

## CI/CD & Deployment

**Hosting / Publishing:**
- NuGet package published to GitHub Packages (`https://nuget.pkg.github.com/ArisenVendetta/index.json`) — configured in `nuget.config`
- Package ID: `RealTimeLogStream`, version `0.3.1.0`
- Symbol package format: `snupkg`

**CI Pipeline:**
- No CI configuration files detected (no `.github/workflows/`, no `azure-pipelines.yml`, no `Jenkinsfile`)

---

## Webhooks & Callbacks

**Incoming:** None

**Outgoing:** None

---

## Third-Party SDK Summary

| Package | Version | Project | Purpose |
|---------|---------|---------|---------|
| `CommunityToolkit.Mvvm` | 8.4.0 | Library + Example | MVVM commands and source generators |
| `CsvHelper` | 33.1.0 | Library | CSV export of log events |
| `Fody` | 6.9.2 | Library + Example | IL weaver host (build-time only) |
| `FluentAssertions` | 7.0.0 | Tests | Fluent assertion syntax |
| `Microsoft.Extensions.DependencyInjection` | 10.0.6 | Example | DI container |
| `Microsoft.Extensions.DependencyInjection.Abstractions` | 10.0.6 | Library | DI abstractions |
| `Microsoft.Extensions.Logging` | 10.0.6 | Library + Example | MEL logging framework |
| `Microsoft.Extensions.Logging.Abstractions` | 10.0.6 | Library + Example | MEL abstractions |
| `Microsoft.NET.Test.Sdk` | 17.12.0 | Tests | MSBuild test SDK |
| `Moq` | 4.20.72 | Tests | Mocking |
| `Newtonsoft.Json` | 13.0.3 | Library + Example | JSON serialization |
| `NLog` | 6.0.1 | Example | File-based logging backend |
| `NLog.Extensions.Logging` | 6.0.1 | Example | NLog MEL adapter |
| `PropertyChanged.Fody` | 4.1.0 | Library + Example | Auto-`INotifyPropertyChanged` weaver |
| `coverlet.collector` | 6.0.2 | Tests | Code coverage |
| `xunit` | 2.9.2 | Tests | Test framework |
| `xunit.runner.visualstudio` | 3.0.0 | Tests | VS test runner adapter |

---

*Integration audit: 2026-05-16*
