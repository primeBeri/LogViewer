using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FluentAssertions;
using LogViewer;
using Microsoft.Extensions.Logging;
using Xunit;

namespace LogViewer.Tests
{
    /// <summary>
    /// Minimal IDispatcher test double that always reports UI-thread access (CheckAccess returns true).
    /// This avoids any actual dispatcher marshalling in unit tests.
    /// </summary>
    internal sealed class FakeDispatcher : IDispatcher
    {
        public bool CheckAccess() => true;
        public T Invoke<T>(Func<T> callback) => callback();
        public Task InvokeAsync(Action callback) { callback(); return Task.CompletedTask; }
        public Task<T> InvokeAsync<T>(Func<T> callback) => Task.FromResult(callback());
    }

    /// <summary>
    /// Tests for the static helpers on <see cref="LogControlViewModel"/> that don't
    /// require a WPF Dispatcher, plus instance-level tests unlocked by the IDispatcher abstraction.
    /// </summary>
    public class LogControlViewModelTests
    {
        // -------- IDispatcher abstraction (ARCH-03) --------

        [Fact]
        public void Constructor_WithFakeDispatcher_DoesNotThrow()
        {
            // Arrange
            var sink = new TestBaseLoggerSink();
            var dispatcher = new FakeDispatcher();

            // Act
            var act = () => new LogControlViewModel(dispatcher, sink);

            // Assert — must not throw; no WPF runtime required
            act.Should().NotThrow();
        }

        [Fact]
        public async Task PauseBuffer_WhenAtMaxLogSize_DoesNotAddNewEntry()
        {
            // Arrange
            var sink = new TestBaseLoggerSink();
            var vm = new LogControlViewModel(new FakeDispatcher(), sink);
            vm.MaxLogSize = 5;
            vm.IsPaused = true;

            // Fill buffer to capacity
            for (int i = 0; i < 5; i++)
            {
                sink.Write(new LogEventArgs(LogLevel.Information, "handle", $"msg{i}", LogColor.Black));
            }

            // Allow async handler to run
            await Task.Delay(50);
            int countAtCapacity = vm.PauseBufferCount;

            // Act — write one more beyond capacity
            sink.Write(new LogEventArgs(LogLevel.Information, "handle", "overflow", LogColor.Black));
            await Task.Delay(50);

            // Assert — count must not exceed MaxLogSize
            vm.PauseBufferCount.Should().Be(countAtCapacity);
            vm.PauseBufferCount.Should().BeLessOrEqualTo(vm.MaxLogSize);
        }

        [Fact]
        public async Task PauseBuffer_WhenBelowMaxLogSize_AddsNewEntry()
        {
            // Arrange
            var sink = new TestBaseLoggerSink();
            var vm = new LogControlViewModel(new FakeDispatcher(), sink);
            vm.MaxLogSize = 10;
            vm.IsPaused = true;

            // Act — write fewer than MaxLogSize entries
            sink.Write(new LogEventArgs(LogLevel.Information, "handle", "msg1", LogColor.Black));
            await Task.Delay(50);

            // Assert — entry was added to the buffer
            vm.PauseBufferCount.Should().Be(1);
        }


        // -------- WildcardToRegex --------

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void WildcardToRegex_NullOrWhitespace_ReturnsMatchAll(string? input)
        {
            LogControlViewModel.WildcardToRegex(input).Should().Be(".*");
        }

        [Fact]
        public void WildcardToRegex_StarWildcard_MatchesAnySequence()
        {
            var pattern = LogControlViewModel.WildcardToRegex("*Service*");
            var regex = new Regex(pattern);

            regex.IsMatch("MyService").Should().BeTrue();
            regex.IsMatch("Service").Should().BeTrue();
            regex.IsMatch("MyServiceX").Should().BeTrue();
            regex.IsMatch("Other").Should().BeFalse();
        }

        [Fact]
        public void WildcardToRegex_QuestionWildcard_MatchesSingleChar()
        {
            var pattern = LogControlViewModel.WildcardToRegex("foo?bar");
            var regex = new Regex(pattern);

            regex.IsMatch("fooXbar").Should().BeTrue();
            regex.IsMatch("foo1bar").Should().BeTrue();
            regex.IsMatch("foobar").Should().BeFalse();   // missing single char
            regex.IsMatch("fooXXbar").Should().BeFalse(); // two chars, not one
        }

        [Fact]
        public void WildcardToRegex_DotIsLiteral()
        {
            // A regex would treat '.' as any char, but in wildcard mode it must be literal.
            var pattern = LogControlViewModel.WildcardToRegex("foo.bar");
            var regex = new Regex(pattern);

            regex.IsMatch("foo.bar").Should().BeTrue();
            regex.IsMatch("fooXbar").Should().BeFalse();
        }

        [Fact]
        public void WildcardToRegex_PipeAsAlternation()
        {
            var pattern = LogControlViewModel.WildcardToRegex("*Service*|*Handler*");
            var regex = new Regex(pattern);

            regex.IsMatch("MyService").Should().BeTrue();
            regex.IsMatch("MyHandler").Should().BeTrue();
            regex.IsMatch("Other").Should().BeFalse();
        }

        [Fact]
        public void WildcardToRegex_SpecialRegexCharsAreEscaped()
        {
            // Brackets, parens, braces are regex metacharacters that must be escaped.
            var pattern = LogControlViewModel.WildcardToRegex("a[b]c");
            var regex = new Regex(pattern);

            regex.IsMatch("a[b]c").Should().BeTrue();
            regex.IsMatch("ab").Should().BeFalse();
        }

        [Fact]
        public void WildcardToRegex_ResultIsAnchored()
        {
            // The wildcard converter anchors with ^...$, so partial matches must fail.
            var pattern = LogControlViewModel.WildcardToRegex("Foo");
            var regex = new Regex(pattern);

            regex.IsMatch("Foo").Should().BeTrue();
            regex.IsMatch("FooBar").Should().BeFalse();
            regex.IsMatch("BarFoo").Should().BeFalse();
        }

        // -------- TEST-01: Pause / Resume --------

        [Fact]
        public async Task PauseAndResume_FlushesBufferedEventsToLogEvents()
        {
            var sink = new TestBaseLoggerSink();
            var vm = new LogControlViewModel(new FakeDispatcher(), sink);
            vm.IsPaused = true;

            sink.Write(new LogEventArgs(LogLevel.Information, "h", "m1", LogColor.Black));
            sink.Write(new LogEventArgs(LogLevel.Information, "h", "m2", LogColor.Black));
            sink.Write(new LogEventArgs(LogLevel.Information, "h", "m3", LogColor.Black));
            await Task.Delay(50);

            vm.LogEvents.Count.Should().Be(0);

            vm.IsPaused = false;
            await Task.Delay(50);

            vm.LogEvents.Count.Should().Be(3);
        }

        // -------- TEST-02: Filters --------

        [Fact]
        public async Task HandleFilter_NonMatchingHandle_NotAddedToLogEvents()
        {
            var sink = new TestBaseLoggerSink();
            var vm = new LogControlViewModel(new FakeDispatcher(), sink);
            vm.LogHandleFilter = LogControlViewModel.WildcardToRegex("specific");

            sink.Write(new LogEventArgs(LogLevel.Information, "other", "msg", LogColor.Black));
            await Task.Delay(50);

            vm.LogEvents.Count.Should().Be(0);
        }

        [Fact]
        public async Task HandleFilter_MatchingHandle_AddedToLogEvents()
        {
            var sink = new TestBaseLoggerSink();
            var vm = new LogControlViewModel(new FakeDispatcher(), sink);
            vm.LogHandleFilter = LogControlViewModel.WildcardToRegex("specific");

            sink.Write(new LogEventArgs(LogLevel.Information, "specific", "msg", LogColor.Black));
            await Task.Delay(50);

            vm.LogEvents.Count.Should().Be(1);
        }

        [Fact]
        public async Task LogLevelFilter_BelowMinimum_NotAddedToLogEvents()
        {
            var sink = new TestBaseLoggerSink();
            var vm = new LogControlViewModel(new FakeDispatcher(), sink);
            vm.LogLevel = LogLevel.Warning;

            sink.Write(new LogEventArgs(LogLevel.Information, "h", "msg", LogColor.Black));
            await Task.Delay(50);

            vm.LogEvents.Count.Should().Be(0);
        }

        [Fact]
        public void InvalidRegexFilter_ReturnsFalse_FilterUnchanged()
        {
            var sink = new TestBaseLoggerSink();
            var vm = new LogControlViewModel(new FakeDispatcher(), sink);

            var result = vm.SetRegexFilterIfValid("(?!");

            result.Should().BeFalse();
            vm.LogHandleFilter.Should().Be(".*");
        }

        // -------- TEST-03: Collection Trimming --------

        [Fact]
        public async Task AddLogs_ExceedingMaxLogSize_TrimsCollection()
        {
            var sink = new TestBaseLoggerSink();
            var vm = new LogControlViewModel(new FakeDispatcher(), sink);
            vm.MaxLogSize = 3;

            for (int i = 0; i < 7; i++)
            {
                sink.Write(new LogEventArgs(LogLevel.Information, "h", $"msg{i}", LogColor.Black));
            }
            await Task.Delay(100);

            vm.LogEvents.Count.Should().BeLessOrEqualTo(3);
        }
    }
}
