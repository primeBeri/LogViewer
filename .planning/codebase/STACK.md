# Technology Stack

**Analysis Date:** 2026-05-16

## Languages

**Primary:**
- C# 13 (implicit via .NET 10 SDK) - All production and test code

**Secondary:**
- XAML - WPF UI definitions (`LogControl.xaml`, `LogWindow.xaml`, `Styles/ButtonStyles.xaml`)
- XML - NLog config (`LogViewerExample/nlog.config`), Fody weavers config (`FodyWeavers.xml`)

## Runtime

**Environment:**
- .NET 10.0 (`net10.0-windows`) - All three projects target this TFM
- Windows-only: `UseWPF=true` locks the TFM to the `-windows` suffix; no cross-platform support

**Package Manager:**
- NuGet (dotnet CLI / Visual Studio)
- Lockfile: Not present (no `packages.lock.json`)
- Custom NuGet source: `https://nuget.pkg.github.com/ArisenVendetta/index.json` (GitHub Packages) configured in `nuget.config`

## Frameworks

**Core:**
- WPF (Windows Presentation Foundation) - UI framework for `LogControl` and `LogWindow` controls; enabled via `<UseWPF>true</UseWPF>` in `LogViewer/LogViewer.csproj` and `LogViewerExample/LogViewerExample.csproj`
- `Microsoft.Extensions.Logging` 10.0.6 - Logging abstractions; `BaseLogger` implements `ILogger` and integrates with `ILoggerFactory` / `ILoggingBuilder`
- `Microsoft.Extensions.DependencyInjection.Abstractions` 10.0.6 - DI abstractions used by extension methods in `BaseLoggerLoggingBuilderExtensions.cs`

**MVVM:**
- `CommunityToolkit.Mvvm` 8.4.0 - `IAsyncRelayCommand`, `RelayCommand`, and `[AddINotifyPropertyChangedInterface]` usage in `LogControlViewModel.cs`
- `PropertyChanged.Fody` 4.1.0 (build-time weaver via `Fody` 6.9.2) - Automatically implements `INotifyPropertyChanged` for properties; applied to `LogControlViewModel` via `[AddINotifyPropertyChangedInterface]`

**Testing:**
- `xunit` 2.9.2 - Test framework
- `xunit.runner.visualstudio` 3.0.0 - Visual Studio test runner adapter
- `Microsoft.NET.Test.Sdk` 17.12.0 - MSBuild test SDK integration
- `Moq` 4.20.72 - Mocking framework
- `FluentAssertions` 7.0.0 - Assertion library
- `coverlet.collector` 6.0.2 - Code coverage collection

**Serialization / Export:**
- `Newtonsoft.Json` 13.0.3 - JSON serialization for log export (`LogExporter.cs`)
- `CsvHelper` 33.1.0 - CSV export of log events via `LogEventArgsMap` class map (`LogExporter.cs`)

**Build / IL Weaving:**
- `Fody` 6.9.2 - IL post-processor host (private asset, build-time only)
- `PropertyChanged.Fody` 4.1.0 - Fody weaver that injects `INotifyPropertyChanged` implementations

**Example App — Additional:**
- `NLog` 6.0.1 - File-based logging backend used alongside LogViewer in `LogViewerExample`
- `NLog.Extensions.Logging` 6.0.1 - Bridges NLog into `Microsoft.Extensions.Logging` (`builder.AddNLog()`)
- `Microsoft.Extensions.DependencyInjection` 10.0.6 - Full DI container used in example app startup

## Build System

**IDE / Toolchain:**
- Visual Studio 2022 (solution format version 17; `VisualStudioVersion = 17.14.36109.1`)
- Solution file: `LogViewer.sln` — three projects: `LogViewer` (library), `LogViewerExample` (WPF exe), `LogViewer.Tests` (xunit)

**Configurations:**
- Debug | Any CPU, Release | Any CPU (plus x64/x86 variants, all mapped to Any CPU builds)

**Code Quality (build-time):**
- `<EnforceCodeStyleInBuild>True</EnforceCodeStyleInBuild>` - Roslyn code style rules enforced at build time (`LogViewer.csproj`)
- `<AnalysisLevel>latest-recommended</AnalysisLevel>` - Latest recommended Roslyn analyzers active
- `<GenerateDocumentationFile>True</GenerateDocumentationFile>` - XML doc file generated on build
- `<Nullable>enable</Nullable>` + `<ImplicitUsings>enable</ImplicitUsings>` - Both enabled in all projects

**Packaging:**
- `LogViewer` project is packaged as a NuGet library: `PackageId=RealTimeLogStream`, version `0.3.1.0`
- `<GeneratePackageOnBuild>True</GeneratePackageOnBuild>` - `.nupkg` and `.snupkg` symbol package emitted on every build
- Assembly signed: No (`<SignAssembly>False</SignAssembly>`)
- `InternalsVisibleTo` granted to `LogViewer.Tests`

## Target Platforms

**Development:**
- Windows 10/11 with .NET 10 SDK installed
- Visual Studio 2022 recommended (WPF designer support)

**Production:**
- Windows desktop only (`net10.0-windows`)
- Delivered as a NuGet library (`RealTimeLogStream`) for embedding in other WPF applications
- No server / cloud deployment model — library is consumed in-process

## Notable Tooling

- **Fody + PropertyChanged.Fody** — Build-time IL weaving that eliminates boilerplate `INotifyPropertyChanged` code. Configured via `FodyWeavers.xml` in each project that uses it.
- **Roslyn Analyzers** — `AnalysisLevel=latest-recommended` activates the full recommended analyzer set; suppression comments (`#pragma warning disable`) are used sparingly and inline.
- **coverlet** — Coverage collected during test runs; no coverage threshold enforced in project files.

---

*Stack analysis: 2026-05-16*
