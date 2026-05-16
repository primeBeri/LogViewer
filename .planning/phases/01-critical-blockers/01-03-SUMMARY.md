---
phase: 1
plan: "01-03"
subsystem: ci
tags: [ci, github-actions, dotnet, multi-tfm, powershell]
dependency_graph:
  requires: []
  provides: [ci-dual-sdk, ci-powershell-publish]
  affects: [.github/workflows/nuget-publish.yml]
tech_stack:
  added: []
  patterns: [setup-dotnet@v4 dual-step, PowerShell Get-ChildItem publish loop]
key_files:
  created: []
  modified:
    - .github/workflows/nuget-publish.yml
decisions:
  - "Two sequential setup-dotnet@v4 steps (8.0.x then 10.0.x) replace single setup-dotnet@v3 to enable multi-TFM builds"
  - "PowerShell Get-ChildItem | ForEach-Object pipeline replaces bash for-loop glob to fix Windows runner publish silently publishing nothing"
  - "actions/checkout and setup-dotnet both upgraded to @v4 (latest stable major)"
metrics:
  duration: "< 5 minutes"
  completed_date: "2026-05-16"
  tasks_completed: 1
  tasks_total: 1
  files_modified: 1
---

# Phase 1 Plan 03: CI Workflow Update Summary

**One-liner:** Two sequential setup-dotnet@v4 steps (net8.0 + net10.0) with PowerShell Get-ChildItem publish loop replacing broken bash glob on Windows runner.

## What Was Done

Replaced the entire contents of `.github/workflows/nuget-publish.yml` to fix two blocking CI issues:

1. **Missing .NET 10 SDK**: The original workflow only installed .NET 8 via a single `setup-dotnet@v3` step, which meant the multi-TFM library (added by plan 01-01) could not be built. The fix installs both SDKs via two sequential `setup-dotnet@v4` steps.

2. **Broken publish glob on Windows**: The original bash for-loop using `**.nupkg` globs does not expand reliably on Windows runners, causing the publish step to silently publish nothing. The fix uses a native PowerShell `Get-ChildItem -Include *.nupkg,*.snupkg -Recurse | ForEach-Object` pipeline, which works correctly on `windows-latest`.

Additional hygiene changes included upgrading `actions/checkout@v3` to `@v4` and removing two diagnostic `pwd` steps.

## Tasks

| Task | Name | Commit | Files |
|------|------|--------|-------|
| 1 | Rewrite nuget-publish.yml with dual SDK setup and PowerShell publish step | 0ae757f | .github/workflows/nuget-publish.yml |

## Acceptance Criteria Results

| Criterion | Expected | Result | Pass |
|-----------|----------|--------|------|
| `setup-dotnet@v4` count | 2 | 2 | PASS |
| `dotnet-version: 8.0.x` matches | 1 | 1 | PASS |
| `dotnet-version: 10.0.x` matches | 1 | 1 | PASS |
| `setup-dotnet@v3` matches | 0 | 0 | PASS |
| `shell: bash` matches | 0 | 0 | PASS |
| `Get-ChildItem` matches | >= 2 | 2 | PASS |
| `actions/checkout@v4` matches | 1 | 1 | PASS |
| `for file in` matches | 0 | 0 | PASS |

## Deviations from Plan

None — plan executed exactly as written.

## Threat Surface Scan

No new network endpoints, auth paths, file access patterns, or schema changes introduced. The secret handling pattern (`${{ secrets.GH_PKG_TOKEN }}`) is unchanged from the original workflow. GitHub Actions masks secrets in logs automatically (T-01-05 accepted, documented in plan threat model).

## Self-Check: PASSED

- File exists: `.github/workflows/nuget-publish.yml` — FOUND
- Task commit `0ae757f` — FOUND in git log
- All 8 acceptance criteria verified and passing
