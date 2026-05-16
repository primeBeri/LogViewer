# LogViewer (RealTimeLogStream) — Project Guide

## Project Overview

`RealTimeLogStream` is a WPF NuGet library that embeds a real-time, filterable log viewer control into WPF applications. It integrates with `Microsoft.Extensions.Logging` as a custom `ILoggerProvider`.

**Package ID:** `RealTimeLogStream`  
**Target Frameworks:** `net8.0-windows`, `net10.0-windows` (multi-target)  
**Status:** Active development toward v1.0.0 public release

## GSD Workflow

This project uses [Get Shit Done (GSD)](https://github.com/anthropics/claude-code) for structured AI-assisted development.

**Planning files:** `.planning/`  
**Current roadmap:** `.planning/ROADMAP.md`  
**Requirements:** `.planning/REQUIREMENTS.md`  
**Project context:** `.planning/PROJECT.md`

### Workflow Commands

```
/gsd-discuss-phase 1    — Gather context and clarify approach for next phase
/gsd-plan-phase 1       — Create detailed execution plan for a phase
/gsd-execute-phase 1    — Execute all plans in a phase
/gsd-progress           — Check current status and what's next
/gsd-verify-work        — Validate delivered features against requirements
```

### Phase Structure

| Phase | Name | Requirements | Status |
|-------|------|--------------|--------|
| 1 | Critical Blockers | TFM-01–03, BUG-01, CI-01–02 | Not started |
| 2 | Architecture Foundations | ARCH-01–04, BUG-02 | Not started |
| 3 | Global State Elimination | STATE-01–04, TEST-01–04 | Not started |
| 4 | Polish and Dependencies | PKG-01–02, UI-01–02 | Not started |

## Architecture

**Solution structure:**
```
LogViewer/            — Main WPF library project (net8.0-windows; net10.0-windows)
LogViewer.Tests/      — xUnit test project
LogViewerExample/     — WPF sample application
```

**Key types:**
- `BaseLoggerSink` — Singleton event hub; all log events flow through here
- `BaseLogger` / `Logger` — `ILogger` implementation; writes to the sink
- `BaseLoggerProvider` — `ILoggerProvider` for DI registration
- `LogControlViewModel` — MVVM ViewModel; filtering, pausing, exporting
- `LogControl` — WPF `UserControl` (the embeddable log viewer)
- `LogCollection` / `ReadOnlyLogCollection` — Thread-safe observable collection

**Two integration patterns (DI is preferred):**
```csharp
// DI pattern (recommended)
builder.AddLogViewer();
// Then place <LogControl /> in XAML

// Inheritance pattern (deprecated in v1.0)
BaseLogger.Initialize(loggerFactory);
public class MyLogger : BaseLogger { ... }
```

## Key Constraints

- **Platform:** Windows-only (`net*-windows`); WPF `Dispatcher` is required
- **Public NuGet:** Semver matters; v1.0.0 is the break point for all breaking changes
- **Breaking changes:** All breaking API changes (Color removal, global state, inheritance deprecation) are bundled into v1.0.0
- **Dependencies:** Minimise — remove `Newtonsoft.Json` in Phase 4; keep `CommunityToolkit.Mvvm`, `Fody`, `CsvHelper`

## Development Standards

- Use `ArgumentNullException.ThrowIfNull` at all public API boundaries
- All WPF UI mutations must go through `DispatchIfNecessaryAsync`
- Thread safety: `LogCollection` uses a lock object; `BaseLoggerSink` uses `ConcurrentQueue` + `Interlocked`
- XML documentation required on all public API members (`GenerateDocumentationFile=True`)
- StyleCop / Roslyn analysis enforced (`AnalysisLevel=latest-recommended`, `EnforceCodeStyleInBuild=True`)

## Testing

```bash
dotnet test LogViewer.Tests/
```

- Framework: xUnit + FluentAssertions + Moq
- `BaseLoggerSink.CreateForTesting()` — creates an isolated sink for tests (bypasses singleton)
- `LogControlViewModel` instance tests require `IDispatcher` abstraction (available after Phase 2)
- No UI automation tests (WPF Dispatcher limitation)

## CI

GitHub Actions workflow: `.github/workflows/nuget-publish.yml`  
Must build and publish packages for both `net8.0-windows` and `net10.0-windows`.
