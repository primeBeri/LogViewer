---
status: partial
phase: 1-critical-blockers
source: [01-VERIFICATION.md]
started: 2026-05-16
updated: 2026-05-16
---

## Current Test

CI smoke test — awaiting human validation.

## Tests

### 1. CI pipeline completes end-to-end (CI-02)

**What to test:** Push a `v*` tag to the repository and confirm the full GitHub Actions workflow completes.

**Specifically verify:**
- The "Restore dependencies" step does NOT return a 401 against the `github` NuGet source
- The "Publish to GitHub Packages" step succeeds
- The published package on GitHub Packages contains both `lib/net8.0-windows7.0/` and `lib/net10.0-windows7.0/` lib folders

**Background:** `nuget.config` contains a literal string `GH_PKG_TOKEN` as `ClearTextPassword` for the `github` NuGet source (pre-existing). All project dependencies are from nuget.org (not GitHub Packages), so `dotnet restore` may silently skip the unauthenticated source. Cannot be confirmed without an actual CI run.

**Remediation if restore fails:** Remove `<packageSourceCredentials>` from `nuget.config` and add a workflow step before restore to inject the real token:
```yaml
- name: Authenticate GitHub Packages
  run: dotnet nuget update source github --password ${{ secrets.GH_PKG_TOKEN }} --store-password-in-clear-text
```

expected: CI workflow passes end-to-end; both TFM lib folders present in published package
result: [pending]

## Summary

total: 1
passed: 0
issues: 0
pending: 1
skipped: 0
blocked: 0

## Gaps
