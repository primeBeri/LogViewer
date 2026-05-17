---
phase: 04-polish-and-dependencies
plan: "01"
subsystem: serialization
tags: [newtonsoft, system-text-json, pkg-01, pkg-02, dependency-reduction]
dependency_graph:
  requires: []
  provides: [system-text-json-serialization]
  affects: [LogExporter, LogEventArgs, LogEventArgsMap, LogViewer.csproj, LogExporterTests]
tech_stack:
  added: [System.Text.Json (inbox .NET runtime — no NuGet install)]
  removed: [Newtonsoft.Json 13.0.3]
  patterns: [JsonSerializer.Serialize with static JsonSerializerOptions, JsonStringEnumConverter, JsonDocument.Parse for test assertions]
key_files:
  modified:
    - LogViewer/LogExporter.cs
    - LogViewer/LogEventArgs.cs
    - LogViewer/LogEventArgsMap.cs
    - LogViewer/LogViewer.csproj
    - LogViewer.Tests/LogExporterTests.cs
decisions:
  - "Use static readonly JsonSerializerOptions field to avoid per-call allocation"
  - "JsonStringEnumConverter added so LogLevel serializes as string name not integer"
  - "LogColor serializes as {A,R,G,B} object (STJ struct behavior) — no test asserts LogColor shape, acceptable per CONTEXT.md"
  - "Test updated to JsonDocument.Parse / RootElement API; using var ensures deterministic disposal"
metrics:
  duration: "5m"
  completed: "2026-05-17T20:19:24Z"
  tasks_completed: 3
  files_modified: 5
---

# Phase 04 Plan 01: Remove Newtonsoft.Json — Migrate to System.Text.Json Summary

**One-liner:** Replaced Newtonsoft.Json 13.0.3 with inbox System.Text.Json across LogExporter, LogEventArgs, LogEventArgsMap, and tests — LogLevel now serializes as "Warning" string via JsonStringEnumConverter.

## Tasks Completed

| Task | Name | Commit | Files |
|------|------|--------|-------|
| 1 | Migrate LogExporter.cs, LogEventArgs.cs, LogEventArgsMap.cs | 67ec74f | LogExporter.cs, LogEventArgs.cs, LogEventArgsMap.cs |
| 2 | Remove Newtonsoft.Json PackageReference from LogViewer.csproj | 67ec74f | LogViewer.csproj |
| 3 | Update LogExporterTests.cs to use System.Text.Json assertions | 67ec74f | LogExporterTests.cs |

(Tasks 1–3 were batched into a single atomic commit as all changes are tightly coupled and verified together.)

## Verification Results

- `dotnet build LogViewer/LogViewer.csproj -p:TargetFramework=net8.0-windows`: **0 errors, 0 warnings** from this change (pre-existing NU1603 and Fody warnings unaffected)
- `dotnet test LogViewer.Tests/`: **131/131 passed**
- `Select-String -Pattern "Newtonsoft"` across all 5 modified files: **0 matches**

## Changes Made

### LogViewer/LogExporter.cs
- Removed `using Newtonsoft.Json;`
- Added `using System.Text.Json;` and `using System.Text.Json.Serialization;`
- Added `private static readonly JsonSerializerOptions _jsonOptions` with `WriteIndented = true` and `JsonStringEnumConverter`
- Replaced `JsonConvert.SerializeObject(logEvents, Formatting.Indented)` with `JsonSerializer.Serialize(logEvents, _jsonOptions)`

### LogViewer/LogEventArgs.cs
- Removed `using Newtonsoft.Json;`
- Added `using System.Text.Json.Serialization;`
- `[JsonIgnore]` attribute on `LogDateTimeFormatted` unchanged textually; now resolves to `System.Text.Json.Serialization.JsonIgnoreAttribute`

### LogViewer/LogEventArgsMap.cs
- Removed unused `using Newtonsoft.Json.Linq;`

### LogViewer/LogViewer.csproj
- Removed `<PackageReference Include="Newtonsoft.Json" Version="13.0.3" />`

### LogViewer.Tests/LogExporterTests.cs
- Removed `using Newtonsoft.Json.Linq;`
- Added `using System.Text.Json;`
- Replaced `JArray.Parse` assertions in `Json_SerializesPublicFields` with `JsonDocument.Parse` / `RootElement` API
- `LogLevel` assertion changed from `int` comparison `(int)LogLevel.Warning` to string comparison `.Should().Be("Warning")`

## Deviations from Plan

None — plan executed exactly as written.

## Known Stubs

None.

## Threat Flags

None. System.Text.Json is an inbox runtime library with no supply-chain surface. No new network endpoints, auth paths, or schema changes were introduced.

## Self-Check: PASSED

- LogViewer/LogExporter.cs: FOUND (contains JsonSerializer.Serialize, no Newtonsoft)
- LogViewer/LogEventArgs.cs: FOUND (contains System.Text.Json.Serialization, no Newtonsoft)
- LogViewer/LogEventArgsMap.cs: FOUND (no Newtonsoft imports)
- LogViewer/LogViewer.csproj: FOUND (no Newtonsoft.Json PackageReference)
- LogViewer.Tests/LogExporterTests.cs: FOUND (contains JsonDocument.Parse, no Newtonsoft.Json.Linq)
- Commit 67ec74f: FOUND
- 131/131 tests: PASSED
