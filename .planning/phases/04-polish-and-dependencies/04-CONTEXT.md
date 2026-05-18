# Phase 4: Polish and Dependencies - Context

**Gathered:** 2026-05-17
**Status:** Ready for planning
**Mode:** Auto-generated (success criteria are technical + one UI behaviour)

<domain>
## Phase Boundary

Remove Newtonsoft.Json as a library dependency, make LogWindow a functional standalone window, and fix the LogControl hardcoded white background so it inherits the host application's WPF theme.

Specifically:
- Replace `Newtonsoft.Json.JsonConvert.SerializeObject` in `LogExporter.GetLogsAsJsonTextAsync` with `System.Text.Json.JsonSerializer.Serialize`
- Replace `[JsonIgnore]` (Newtonsoft) on `LogEventArgs.LogDateTimeFormatted` with `[System.Text.Json.Serialization.JsonIgnore]`
- Remove `using Newtonsoft.Json.Linq;` from `LogEventArgsMap.cs` (unused import)
- Remove `<PackageReference Include="Newtonsoft.Json">` from `LogViewer.csproj`
- Update `LogExporterTests.cs` to parse JSON with System.Text.Json instead of `Newtonsoft.Json.Linq.JArray`
- Embed `<local:LogControl />` inside `LogWindow.xaml` so `new LogWindow().Show()` shows the live log viewer
- Change `LogControl.xaml` `<Grid Background="White">` to `<Grid Background="{DynamicResource {x:Static SystemColors.WindowBrushKey}}">` to inherit host theme

</domain>

<decisions>
## Implementation Decisions

### Claude's Discretion
All implementation choices are at Claude's discretion — technical phase.

Key constraints:
- `System.Text.Json.JsonSerializer.Serialize` must produce **indented** output (ROADMAP requirement: "indented, enum strings")
- `LogLevel` enum must serialize as **string** ("Warning", not 3) — use `JsonStringEnumConverter` in `JsonSerializerOptions`
- `LogDateTimeFormatted` must still be excluded from JSON output — the `[System.Text.Json.Serialization.JsonIgnore]` attribute achieves this
- `LogColor` does not have a built-in System.Text.Json converter — since it's a struct with public properties, System.Text.Json will serialize its ARGB fields by default (A, R, G, B as bytes). This is acceptable; the format changes from `"LogColor": "#FFFFAA00"` (Newtonsoft string) to `"LogColor": {"A":255,"R":255,"G":170,"B":0}` (STJ object). Tests must account for this change.
- `LogEventArgs` JSON output: the existing test `Json_SerializesPublicFields` asserts `((int)token["LogLevel"]!).Should().Be((int)LogLevel.Warning)` — this must change to assert `"Warning"` string when using `JsonStringEnumConverter`
- The existing test `Json_EmptyInput_ProducesEmptyArray` asserts `"[]"` — System.Text.Json produces `[]` for empty collections too; no change needed
- The existing `Json_IsIndented` test asserts the output contains `Environment.NewLine` — this holds for STJ WriteIndented=true
- LogWindow.xaml: embed `<local:LogControl />` inside the `<Grid>` to make `new LogWindow().Show()` functional; the code-behind requires no changes
- LogControl.xaml: change `Background="White"` on line 23 to `Background="{DynamicResource {x:Static SystemColors.WindowBrushKey}}"` — this is the WPF standard for inheriting the window background colour

### Test changes required (LogExporterTests.cs)
The test file currently uses `Newtonsoft.Json.Linq.JArray` to parse JSON output. After migration:
- Replace `using Newtonsoft.Json.Linq;` with `using System.Text.Json;`
- In `Json_SerializesPublicFields`: replace `JArray.Parse(sb.ToString())` with `JsonDocument.Parse(sb.ToString()).RootElement`; the LogLevel assertion changes from `((int)token["LogLevel"]!)` to `token.GetProperty("LogLevel").GetString()` asserting `"Warning"`; the LogHandle/LogText assertions remain equivalent
- In `Json_OmitsJsonIgnoredProperties`: string-contains check still works; no structural change needed
- The `LogViewer.Tests.csproj` also references `Newtonsoft.Json` — check and remove that PackageReference too if present

</decisions>

<code_context>
## Existing Code Insights

### Newtonsoft.Json usage (4 files)
- `LogViewer/LogExporter.cs:4` — `using Newtonsoft.Json;` + `JsonConvert.SerializeObject(logEvents, Formatting.Indented)`
- `LogViewer/LogEventArgs.cs:10` — `using Newtonsoft.Json;` + `[JsonIgnore]` on `LogDateTimeFormatted`
- `LogViewer/LogEventArgsMap.cs:1` — `using Newtonsoft.Json.Linq;` (unused — file uses CsvHelper only)
- `LogViewer.Tests/LogExporterTests.cs:7` — `using Newtonsoft.Json.Linq;` + `JArray.Parse`

### LogWindow.xaml (currently empty)
```xml
<Window ...>
    <Grid>
        <!-- empty — ships as dead shell -->
    </Grid>
</Window>
```
The code-behind `LogWindow.xaml.cs` is a minimal partial class with only `InitializeComponent()`. No changes to code-behind needed.

### LogControl.xaml background (line 23)
```xml
<Grid Background="White">
```
This hardcoding means the control always has a white background even in dark-themed host apps.

### System.Text.Json replacement for LogExporter
```csharp
using System.Text.Json;
using System.Text.Json.Serialization;

private static readonly JsonSerializerOptions _jsonOptions = new()
{
    WriteIndented = true,
    Converters = { new JsonStringEnumConverter() }
};

public static async Task<StringBuilder> GetLogsAsJsonTextAsync(IEnumerable<LogEventArgs> logEvents)
{
    ArgumentNullException.ThrowIfNull(logEvents, paramName: nameof(logEvents));
    return await Task.Run(() => new StringBuilder(JsonSerializer.Serialize(logEvents, _jsonOptions)));
}
```

### System.Text.Json attribute for LogEventArgs
```csharp
using System.Text.Json.Serialization;
// Change: [JsonIgnore] → [JsonIgnore]  (same attribute name, different namespace)
[JsonIgnore] public string LogDateTimeFormatted => ...
```
(The Newtonsoft `using Newtonsoft.Json;` is removed; the System.Text.Json one is added.)

### Files to create
None.

### Files to modify (PKG)
- `LogViewer/LogViewer.csproj` — remove Newtonsoft.Json PackageReference
- `LogViewer/LogExporter.cs` — System.Text.Json migration
- `LogViewer/LogEventArgs.cs` — swap `[JsonIgnore]` attribute namespace
- `LogViewer/LogEventArgsMap.cs` — remove unused Newtonsoft.Json.Linq import
- `LogViewer.Tests/LogExporterTests.cs` — update JSON assertions

### Files to modify (UI)
- `LogViewer/LogWindow.xaml` — embed `<local:LogControl />`
- `LogViewer/LogControl.xaml` — change `Background="White"`

</code_context>

<specifics>
## Specific Ideas

- `JsonSerializerOptions` should be a static readonly field on `LogExporter` to avoid allocating a new options object on every call — this is the standard performance pattern for STJ
- `LogEventArgsMap.cs` — the `using Newtonsoft.Json.Linq;` is definitely unused; removing it is safe
- After removing Newtonsoft.Json, run `dotnet build` to confirm no remaining references
- `LogWindow.xaml` needs `xmlns:local="clr-namespace:LogViewer"` — already present in the template; just add `<local:LogControl />` inside the Grid
- The WPF `SystemColors.WindowBrushKey` is the standard background brush for windows; it correctly adapts to dark mode and custom themes set by the host app

</specifics>

<deferred>
## Deferred Ideas

None — all four requirements (PKG-01, PKG-02, UI-01, UI-02) are in scope.

</deferred>
