# Testing Patterns

**Analysis Date:** 2026-05-16

## Test Framework

**Runner:** xUnit 2.9.2
- Config: via `LogViewer.Tests.csproj` (`<IsTestProject>true</IsTestProject>`)
- Runner: `xunit.runner.visualstudio` 3.0.0

**Assertion Library:** FluentAssertions 7.0.0

**Mocking:** Moq 4.20.72

**Coverage Collector:** `coverlet.collector` 6.0.2

**Run Commands:**
```bash
dotnet test                                         # Run all tests
dotnet test --logger trx                            # Run with TRX output for CI
dotnet test --collect:"XPlat Code Coverage"         # Run with coverage
```

**Target Framework:** `net10.0-windows` (requires Windows due to WPF dependency; STA thread is needed for Dispatcher-dependent code)

---

## Test Project Structure

**Location:** `LogViewer.Tests/` — a dedicated sibling project, not co-located with source

**Naming convention:** `{TypeUnderTest}Tests.cs`

```
LogViewer.Tests/
├── BaseLoggerLoggingBuilderExtensionsTests.cs
├── BaseLoggerProviderTests.cs
├── BaseLoggerSinkTests.cs
├── BaseLoggerTests.cs
├── GlobalStateCollection.cs          ← xUnit collection fixture for isolation
├── IntegrationTests.cs
├── LogCollectionTests.cs
├── LogControlViewModelTests.cs
├── LogEventArgsTests.cs
├── LogExporterTests.cs
└── TestBaseLoggerSink.cs             ← shared test double (fake)
```

**Namespace:** `LogViewer.Tests` (matches assembly name)

**Access to internals:** `LogViewer.csproj` includes `<InternalsVisibleTo Include="LogViewer.Tests" />`, allowing tests to construct `BaseLogger` via its `internal` DI constructor and call `internal` methods.

---

## Types of Tests Present

### Unit Tests (majority)
Tests that exercise a single class in isolation, substituting dependencies via `TestBaseLoggerSink` (fake) or `Mock<T>` (Moq). These are the bulk of the suite:
- `BaseLoggerTests.cs` — `BaseLogger` instance behaviour (DI constructor path)
- `BaseLoggerProviderTests.cs` — provider caching, color assignment, disposal
- `BaseLoggerSinkTests.cs` — queue behaviour, event raising, trimming, concurrency
- `LogCollectionTests.cs` — `LogCollection` CRUD, notification modes, edge cases
- `LogEventArgsTests.cs` — `FormatLogMessage` substitutions, equality, `ToString`
- `LogExporterTests.cs` — JSON/TXT/CSV serialisation, format round-trips
- `LogControlViewModelTests.cs` — static helpers (`WildcardToRegex`) only

### Integration Tests
`IntegrationTests.cs` — tests the full pipeline from `ILoggerFactory` + `BaseLoggerProvider` through to events arriving in the sink:
- Factory-level wiring and `SetCategoryColors`
- DI container resolution of `ILogger<T>` via `AddLogViewer()`
- Inner-factory pass-through (dual output to both LogViewer and a wrapped factory)
- `LogEventArgs` formatting end-to-end

### No E2E / UI Tests
The WPF `LogControl` UserControl, `LogWindow`, and `LogControlViewModel` dispatcher-dependent behaviour are **not tested**. The `LogControlViewModelTests` class tests only the static `WildcardToRegex` helper. A comment in that file explicitly defers instance-level VM tests to a future version.

---

## Test Coverage Areas

### Well-Covered
| Area | Test File |
|------|-----------|
| `LogCollection` CRUD, batch ops, notification modes | `LogCollectionTests.cs` |
| `LogEventArgs.FormatLogMessage` (all placeholders, case-insensitivity, unknowns) | `LogEventArgsTests.cs` |
| `LogExporter` (JSON, TXT, CSV; null guards; edge-case strings) | `LogExporterTests.cs` |
| `BaseLoggerSink` queue, trimming, event firing, concurrency | `BaseLoggerSinkTests.cs` |
| `BaseLoggerProvider` caching, color mapping, disposal | `BaseLoggerProviderTests.cs` |
| `AddLogViewer` DI extensions (options application, null guards) | `BaseLoggerLoggingBuilderExtensionsTests.cs` |
| `BaseLogger` DI-path instance (sink write, pass-through, log level, timestamp) | `BaseLoggerTests.cs` |
| `LogControlViewModel.WildcardToRegex` static helper | `LogControlViewModelTests.cs` |
| Full pipeline (factory → provider → sink → event) | `IntegrationTests.cs` |

### Not Covered / Gaps
| Area | Gap | Risk |
|------|-----|------|
| `LogControlViewModel` instance behaviour | Dispatcher dependency makes testing hard without STA; explicitly deferred | Medium — filter logic, pause/resume, `UpdateVisibleLogsAsync`, `AddAndTrimLogEventsIfNeededAsync` are untested |
| `LogControl` (WPF UserControl) | No XAML/UI tests; codebehind is entirely untested | Low for logic, high for wiring bugs |
| `BaseLogger` inheritance-pattern constructor | Requires `BaseLogger.Initialize()` with a live `ILoggerFactory`; no test exercises this path | Medium — static init/shutdown guard, `SanitizeHandle` via the public ctor |
| `BaseLogger.SanitizeHandle` static method | Called indirectly but no dedicated `[Theory]` over character-exclusion rules | Low |
| `ReadOnlyLogCollection` | No direct tests; covered implicitly via `LogControlViewModel` | Low — forwarding constructor is trivial |
| `LogEventArgs` concurrency | `ThreadId` capture, reference equality under concurrent construction | Low |
| `LogControl` Dependency Property callbacks | `MaxLogSize`, `HandleFilter`, `LogDisplayFormat` DP change callbacks untested | Medium |
| Export cancellation / `ExportLogResult` error path | `GetLogExportFilePathWithName` shows a `SaveFileDialog` — impossible to test without UI mocking | Low for cancellation path, medium for error path |
| `BaseLogger` static settings (inheritance pattern) | `LogUTCTime`, `ExcludeCharsFromHandle`, `DefaultLogDisplayFormat` setters untested in isolation | Low — some settings exercised via integration tests |

---

## Test Double Patterns

### Fake: `TestBaseLoggerSink`
Located in `LogViewer.Tests/TestBaseLoggerSink.cs`. Implements `IBaseLoggerSink` directly — no mocking framework involved. Captures all written events in `ReceivedEvents` (a plain `List<LogEventArgs>`), enqueues them in a `ConcurrentQueue`, and fires `LogReceived` synchronously.

```csharp
public class TestBaseLoggerSink : IBaseLoggerSink
{
    public List<LogEventArgs> ReceivedEvents { get; } = new();
    public event LogEvent? LogReceived;
    public int MaxQueueSize { get; set; } = 1000;
    public ConcurrentQueue<LogEventArgs> LogQueue { get; } = new();

    public void Write(LogEventArgs logEvent)
    {
        if (logEvent is null) return;
        ReceivedEvents.Add(logEvent);
        LogQueue.Enqueue(logEvent);
        LogReceived?.Invoke(this, logEvent);
    }
}
```

This fake is used in the vast majority of test classes. It is the primary seam for inspecting what the system under test wrote.

### Moq: Selective use for `ILogger` pass-through
Moq is used only in `BaseLoggerTests.cs`, exclusively to verify that `BaseLogger` calls through to an `ILogger` inner logger with the correct `LogLevel` and `EventId`. Two mocks appear in the file:
```csharp
var innerLogger = new Mock<ILogger>();
innerLogger.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
// ...
innerLogger.Verify(l => l.Log(LogLevel.Warning, ...), Times.Once);
```

### Test Helper: `CreateProvider()`
A private helper factory method in `BaseLoggerTests` reduces construction boilerplate:
```csharp
private BaseLoggerProvider CreateProvider() => new(_sink, null);
```

### Test-Specific Factory Method on Production Class
`BaseLoggerSink` exposes a static factory specifically for tests to obtain non-singleton instances, avoiding cross-test contamination from the singleton:
```csharp
// Production code
public static BaseLoggerSink CreateForTesting() => new();

// Usage in tests
var sink = BaseLoggerSink.CreateForTesting();
```

### Local Helper: `Make()` Factory Functions
Several test classes define a private static `Make()` method to construct `LogEventArgs` with sensible defaults and only specify the fields relevant to the test:
```csharp
private static LogEventArgs Make(
    LogLevel level = LogLevel.Information,
    string handle = "TestHandle",
    string message = "Test message",
    Color? color = null,
    DateTime? timestamp = null)
    => new(level, handle, message, color ?? Colors.Black)
    {
        LogDateTime = timestamp ?? new DateTime(2026, 5, 4, 12, 30, 45, 678, DateTimeKind.Utc)
    };
```
This pattern appears in `LogEventArgsTests.cs`, `LogExporterTests.cs`, and `LogCollectionTests.cs`.

### Inner Test Classes (Integration Helpers)
`IntegrationTests.cs` defines two private inner classes (`TestService`, `TestLoggerProvider`/`TestLogger`) that are purely local to those tests. They are not reused.

---

## xUnit Collection Fixtures

A named collection `"GlobalState"` is defined in `GlobalStateCollection.cs` using `[CollectionDefinition]`. Test classes that mutate or read `BaseLoggerSink.Instance` or `BaseLogger`'s static settings are decorated with `[Collection(GlobalStateCollection.Name)]` to force sequential execution and prevent race conditions:

```csharp
[CollectionDefinition(Name)]
public sealed class GlobalStateCollection
{
    public const string Name = "GlobalState";
}

// Applied to:
[Collection(GlobalStateCollection.Name)]
public class BaseLoggerSinkTests { ... }

[Collection(GlobalStateCollection.Name)]
public class BaseLoggerLoggingBuilderExtensionsTests { ... }

[Collection(GlobalStateCollection.Name)]
public class IntegrationTests { ... }

[Collection(GlobalStateCollection.Name)]
public class LogEventArgsTests { ... }

[Collection(GlobalStateCollection.Name)]
public class LogExporterTests { ... }
```

Classes that do **not** carry this attribute (e.g., `LogCollectionTests`, `BaseLoggerProviderTests`, `BaseLoggerTests`, `LogControlViewModelTests`) run in parallel with each other and with other collections.

---

## Test Structure Pattern

All test methods follow the `// Arrange / // Act / // Assert` comment structure:

```csharp
[Fact]
public void Log_WritesToSink()
{
    // Arrange
    var provider = CreateProvider();
    var logger = new BaseLogger("TestCategory", Colors.Blue, _sink, null, provider);

    // Act
    logger.LogInformation("Test message");

    // Assert
    _sink.ReceivedEvents.Should().HaveCount(1);
    _sink.ReceivedEvents[0].LogHandle.Should().Be("TestCategory");
    _sink.ReceivedEvents[0].LogText.Should().Contain("Test message");
}
```

Test method names follow `MethodUnderTest_Condition_ExpectedOutcome` or `MethodUnderTest_Description` convention.

`[Theory]` + `[InlineData]` is used sparingly, only where the same logical assertion needs to hold across a small number of literal values (e.g., case-insensitivity variants of a placeholder name, or null/empty/whitespace permutations).

---

## Async Test Patterns

Async tests use `async Task` return type (not `async void`). One test uses a `Task.Delay` to allow a fire-and-forget async event to propagate before asserting:

```csharp
[Fact]
public async Task Write_RaisesLogReceivedEvent()
{
    // ...
    sink.Write(logEvent);

    // Assert - give async event time to fire
    await Task.Delay(100);
    receivedEvent.Should().Be(logEvent);
}
```

This is the only `Task.Delay`-based timing dependency in the suite; all other async tests await directly and do not rely on timing.

---

## Coverage Configuration

No coverage thresholds are enforced in the project file. `coverlet.collector` is referenced as a build-time tool only. Coverage collection must be triggered explicitly via `dotnet test --collect:"XPlat Code Coverage"`. No coverage gate runs in the GitHub Actions publish workflow.
