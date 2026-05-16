---
phase: 1
plan: 01-01
subsystem: build
tags: [multi-target, csproj, tfm, net8, net10, nuget]
dependency_graph:
  requires: []
  provides: [net8.0-windows-target, net10.0-windows-target, multi-tfm-nupkg]
  affects: [LogViewer/LogViewer.csproj, LogViewer/BaseLoggerSink.cs]
tech_stack:
  added: []
  patterns: [conditional-itemgroup-per-tfm, msbuild-condition-targetframework]
key_files:
  created: []
  modified:
    - LogViewer/LogViewer.csproj
    - LogViewer/BaseLoggerSink.cs
decisions:
  - "Use conditional ItemGroup blocks (Condition on TargetFramework) for Microsoft.Extensions.* version selection — chosen per CONTEXT.md decision"
  - "Version 8.0.6 for net8.0-windows, 10.0.6 for net10.0-windows — aligns minor version numbers"
  - "Fixed Task.WhenAll(Span<Task>) .NET 8 incompatibility using array slice [..length] instead — preserves ArrayPool usage, avoids #if guards"
metrics:
  duration: "~8 minutes"
  completed: "2026-05-16"
  tasks_completed: 1
  tasks_total: 1
---

# Phase 1 Plan 01-01: Multi-Target TFM Conversion Summary

**One-liner:** Converted LogViewer.csproj to dual-target net8.0-windows;net10.0-windows with per-TFM Microsoft.Extensions.* version conditionals (8.0.6/10.0.6), and fixed a .NET 8 incompatible `Task.WhenAll(Span<Task>)` call in BaseLoggerSink.

## Tasks Completed

| Task | Name | Commit | Files |
|------|------|--------|-------|
| 1 | Switch to multi-target and add conditional package version blocks | 894a22a | LogViewer/LogViewer.csproj, LogViewer/BaseLoggerSink.cs |

## Verification Results

All 7 acceptance criteria passed:

1. `grep -c "TargetFrameworks" LogViewer/LogViewer.csproj` returns `1` — PASS
2. `grep "TargetFrameworks" ...` contains `net8.0-windows;net10.0-windows` — PASS
3. `grep -c "TargetFramework>" ...` returns `0` (singular element gone) — PASS
4. `net8.0-windows` lines show Condition and `8.0.6` version — PASS
5. `net10.0-windows` lines show Condition and `10.0.6` version — PASS
6. `dotnet build --configuration Release` exits 0 with "Build succeeded" — PASS
7. `bin/Release/net8.0-windows/` and `bin/Release/net10.0-windows/` both exist — PASS

Additional verification (from plan's `<verification>` section):
- `LogViewer/bin/Release/net8.0-windows/LogViewer.dll` — EXISTS
- `LogViewer/bin/Release/net10.0-windows/LogViewer.dll` — EXISTS
- `dotnet pack` produces `RealTimeLogStream.0.3.1.nupkg` — PASS
- nupkg contains `lib/net8.0-windows7.0/` and `lib/net10.0-windows7.0/` — PASS

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Fixed Task.WhenAll(Span<Task>) incompatibility with .NET 8**

- **Found during:** Task 1, first build attempt
- **Issue:** `BaseLoggerSink.cs` line 130 used `Task.WhenAll(tasks.AsSpan(0, length))` — the `Task.WhenAll(ReadOnlySpan<Task>)` overload was introduced in .NET 10 and is not available in .NET 8, causing CS1503 compilation error.
- **Fix:** Changed to `Task.WhenAll(tasks[..length])` — array range expression creates a `Task[]` slice that matches the `IEnumerable<Task>` overload available on all .NET versions. The ArrayPool allocation pattern is preserved; only the WhenAll call site changes.
- **Files modified:** `LogViewer/BaseLoggerSink.cs` (line 130)
- **Commit:** 894a22a (included in same commit as csproj change)

### NU1603 Warnings (Non-blocking)

NuGet resolved `Microsoft.Extensions.*` version `9.0.0` instead of `8.0.6` for the `net8.0-windows` target because `8.0.6` is not in the local NuGet cache but `9.0.0` satisfies the `>= 8.0.6` minimum version constraint. These are NU1603 warnings (not errors). The resolved `9.0.0` packages are fully compatible with net8.0-windows. The build succeeds and the structural requirement (conditional version blocks) is correctly in place. This is a developer-environment cache artifact, not a project correctness issue.

## Known Stubs

None.

## Threat Flags

None. No new network endpoints, auth paths, file access patterns, or schema changes introduced.

## Self-Check: PASSED

- `LogViewer/LogViewer.csproj` — FOUND and verified correct
- `LogViewer/BaseLoggerSink.cs` — FOUND with fix applied
- Commit `894a22a` — FOUND in git log
- `LogViewer/bin/Release/net8.0-windows/LogViewer.dll` — FOUND
- `LogViewer/bin/Release/net10.0-windows/LogViewer.dll` — FOUND
- `nupkgs/RealTimeLogStream.0.3.1.nupkg` contains both lib TFM folders — VERIFIED
