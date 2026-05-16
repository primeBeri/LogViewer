# Phase 1: Critical Blockers - Context

**Gathered:** 2026-05-16
**Status:** Ready for planning

<domain>
## Phase Boundary

Make the NuGet package installable into both .NET 8 and .NET 10 WPF applications, fix the export success-flag bug, and correct the CI pipeline to build and publish both TFMs. No architecture changes — pure compatibility, correctness, and CI work.

</domain>

<decisions>
## Implementation Decisions

### Package Version Strategy
- `Microsoft.Extensions.DependencyInjection.Abstractions`, `Microsoft.Extensions.Logging`, and `Microsoft.Extensions.Logging.Abstractions` use conditional versions: `8.0.x` when building for `net8.0-windows`, `10.0.x` when building for `net10.0-windows`
- Implement via `<ItemGroup Condition="'$(TargetFramework)' == 'net8.0-windows'">` / `<ItemGroup Condition="'$(TargetFramework)' == 'net10.0-windows'">` in the csproj
- All other packages (CommunityToolkit.Mvvm, CsvHelper, Fody, PropertyChanged.Fody, Newtonsoft.Json) remain at current versions — they support both TFMs without conditional references

### CI Multi-SDK Strategy
- Install both .NET 8 and .NET 10 SDKs via two sequential `actions/setup-dotnet@v4` steps in the same job
- Run `dotnet test` once — the test project targets a single framework and does not require per-TFM iteration
- The `dotnet build` and `dotnet pack` steps run against the library project and will produce both TFM outputs from a single invocation (multi-target build)

### Claude's Discretion
- Exact patch versions for `8.0.x` packages: align with latest available at time of change (e.g., `8.0.6` to match the current `10.0.6` minor)
- Export bug fix placement: set `output.Success = true` immediately after `writer.FlushAsync()` succeeds, before the `return output` statement
- README framework update: update version badge, framework statement, and Installation section to list both `net8.0-windows` and `net10.0-windows`

</decisions>

<code_context>
## Existing Code Insights

### Reusable Assets
- `ExportLogResult.cs` — `Success` property already exists as `bool`, just never set to `true`; one-line fix
- `LogControlViewModel.ExportLogsAsync` — the happy path runs: get path → build content → write file → flush; `Success = true` belongs after flush

### Established Patterns
- `LogViewer.csproj` uses `<PropertyGroup Condition="...">` for Debug/Release config — same MSBuild condition pattern applies to per-TFM `<ItemGroup>` blocks
- CI workflow is sequential (no matrix); keeping that pattern for SDK installs

### Integration Points
- `<TargetFramework>net10.0-windows</TargetFramework>` → `<TargetFrameworks>net8.0-windows;net10.0-windows</TargetFrameworks>` (element renamed to plural)
- `nuget-publish.yml` currently has one `setup-dotnet` step with `dotnet-version: 8.0.x`; needs a second step for `10.0.x` and the build steps will work unchanged since `dotnet build` multi-target is automatic

</code_context>

<specifics>
## Specific Ideas

- Use `Microsoft.Extensions.*` version `8.0.6` for `net8.0-windows` (aligns minor with current `10.0.6`)
- The `<TargetFrameworks>` change is the only csproj structural change needed; no source code changes are required for multi-targeting itself (no `#if` guards needed — WPF APIs used are available on both TFMs)
- README: update header badge from "**.NET 8.0 Windows**" to "**.NET 8+ Windows**", add both frameworks to the Installation section

</specifics>

<deferred>
## Deferred Ideas

- Publishing to NuGet.org (currently GitHub Packages only) — out of scope for Phase 1; CI correctness only
- Updating `AssemblyVersion` / `FileVersion` to pre-release v1.0 numbering — Phase 4 concern
- Adding a `global.json` for SDK pinning — not required; explicit setup-dotnet steps suffice

</deferred>
