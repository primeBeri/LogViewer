---
phase: 1
plan: "01-02"
subsystem: "LogControlViewModel / Export"
tags: [bug-fix, export, correctness]
dependency_graph:
  requires: []
  provides: ["ExportLogResult.Success = true on happy path"]
  affects: ["LogViewer/LogControlViewModel.cs"]
tech_stack:
  added: []
  patterns: []
key_files:
  created: []
  modified:
    - LogViewer/LogControlViewModel.cs
decisions:
  - "Set output.Success = true immediately after writer.FlushAsync(cancellationToken) — before the closing brace of the try block — so cancel and exception paths remain false"
metrics:
  duration: "< 5 minutes"
  completed: "2026-05-16T19:42:42Z"
---

# Phase 1 Plan 02: ExportLogResult.Success Bug Fix Summary

## One-liner

Single-line correctness fix: `output.Success = true` inserted after `writer.FlushAsync` in `ExportLogsAsync`, making the happy path reliably signal success to callers.

## What Was Done

### Task 1: Set output.Success = true on the export happy path

`ExportLogsAsync` initialised `ExportLogResult.Success = false` and never set it to `true`, even after a successful file write. Callers checking `result.Success` always received `false` regardless of outcome.

**Fix:** Inserted one line after `await writer.FlushAsync(cancellationToken);` inside the `try` block:

```csharp
output.Success = true;
```

The resulting sequence in the try block:

```csharp
await using var writer = new StreamWriter(filePath, append: false, encoding: Encoding.UTF8);
await writer.WriteAsync(contents.ToString().AsMemory(), cancellationToken: cancellationToken);
await writer.FlushAsync(cancellationToken);
output.Success = true;
```

Cancel path (early return when `filePath` is null/whitespace) and exception path (catch block) both continue to produce `Success = false`, which is correct.

## Verification

- `grep -n "output.Success = true" LogViewer/LogControlViewModel.cs` — returns exactly one line (line 700)
- `Success = false` exists only in the object initialiser (`Success = false` inside `new ExportLogResult { ... }`) — no additional false-assignments
- `dotnet test` — **114 passed, 0 failed, 0 skipped**

## Commits

| Task | Description | Commit |
|------|-------------|--------|
| 1 | Insert output.Success = true after FlushAsync | a1c3cbc |

## Deviations from Plan

None — plan executed exactly as written. 

Note: Acceptance criterion 3 specified `grep -c "output.Success = false"` returning 1, but the actual initialiser uses the object-initialiser syntax `Success = false` (without the `output.` prefix), so the grep returns 0. The code is correct — there is exactly one false-initialisation and zero extra false-assignments. This is a grep-pattern discrepancy in the plan, not a code deficiency.

## Known Stubs

None.

## Threat Flags

None — this fix only sets a boolean flag after a successful flush. No new network endpoints, auth paths, file access patterns, or schema changes were introduced.

## Self-Check: PASSED

- [x] `LogViewer/LogControlViewModel.cs` exists and contains `output.Success = true` at line 700
- [x] Commit `a1c3cbc` exists: `git log --oneline | grep a1c3cbc`
- [x] 114 tests pass, 0 failures
- [x] No STATE.md or ROADMAP.md modifications
