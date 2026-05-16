# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-05-16)

**Core value:** A plug-and-play WPF log viewer that any .NET 8 or .NET 10 application can embed via NuGet with a single `AddLogViewer()` call and a `<LogControl />` element — no boilerplate, no friction.
**Current focus:** Phase 1 — Critical Blockers

## Current Position

Phase: 1 of 4 (Critical Blockers)
Plan: 0 of TBD in current phase
Status: Ready to plan
Last activity: 2026-05-16 — Roadmap created; all 23 v1 requirements mapped across 4 phases

Progress: [░░░░░░░░░░] 0%

## Performance Metrics

**Velocity:**
- Total plans completed: 0
- Average duration: -
- Total execution time: -

**By Phase:**

| Phase | Plans | Total | Avg/Plan |
|-------|-------|-------|----------|
| - | - | - | - |

**Recent Trend:**
- Last 5 plans: -
- Trend: -

*Updated after each plan completion*

## Accumulated Context

### Decisions

Decisions are logged in PROJECT.md Key Decisions table.
Recent decisions affecting current work:

- (Roadmap): Multi-target `net8.0-windows;net10.0-windows` chosen over lowering to net8 only — keeps .NET 10 API access while enabling .NET 8 consumers immediately
- (Roadmap): All breaking changes (Color removal, global state, inheritance deprecation) bundled into single v1.0.0 — avoids repeated semver major bumps on a pre-release library

### Pending Todos

None yet.

### Blockers/Concerns

- Phase 2 depends on `IDispatcher` abstraction being in place before `LogControlViewModel` tests can be written (TEST-01 through TEST-03 are in Phase 3 but require Phase 2's ARCH-03/04 to be complete first — dependency is correctly ordered)

## Deferred Items

| Category | Item | Status | Deferred At |
|----------|------|--------|-------------|
| Observability | OBS-01: Queue-depth metric / dropped-message counter | v2 | Roadmap creation |
| Observability | OBS-02: TaskScheduler.UnobservedTaskException docs | v2 | Roadmap creation |
| Performance | PERF-01: Skip().ToList() → GetRange() in UpdateVisibleLogsAsync | v2 | Roadmap creation |
| Structured Logging | STRUCT-01: Structured logging properties in LogEventArgs | v2 | Roadmap creation |
| OpenTelemetry | OTEL-01: Optional OTel companion package | v2 | Roadmap creation |

## Session Continuity

Last session: 2026-05-16
Stopped at: Roadmap, STATE, and REQUIREMENTS traceability written. No phases planned yet.
Resume file: None
