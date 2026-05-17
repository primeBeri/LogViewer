using System.Globalization;
using FluentAssertions;
using LogViewer;
using Microsoft.Extensions.Logging;
using Xunit;

namespace LogViewer.Tests
{
    [Collection(GlobalStateCollection.Name)]
    public class LogEventArgsTests
    {
        private static LogEventArgs Make(
            LogLevel level = LogLevel.Information,
            string handle = "TestHandle",
            string message = "Test message",
            LogColor? color = null,
            DateTime? timestamp = null)
            => new(level, handle, message, color ?? LogColor.Black)
            {
                LogDateTime = timestamp ?? new DateTime(2026, 5, 4, 12, 30, 45, 678, DateTimeKind.Utc)
            };

        // -------- FormatLogMessage --------

        [Fact]
        public void FormatLogMessage_NullFormat_UsesDefault()
        {
            var e = Make();
            // Default is BaseLogger.LogExportFormat.
            string output = e.FormatLogMessage(null);

            // The default export format ends with {message}, so the message text should appear.
            output.Should().Contain(e.LogText);
            output.Should().Contain(e.LogHandle);
        }

        [Fact]
        public void FormatLogMessage_WhitespaceFormat_UsesDefault()
        {
            var e = Make();
            string output = e.FormatLogMessage("   ");
            output.Should().Contain(e.LogText);
        }

        [Fact]
        public void FormatLogMessage_SubstitutesTimestamp()
        {
            var e = Make();
            string output = e.FormatLogMessage("{timestamp}");
            output.Should().Be(e.LogDateTimeFormatted);
        }

        [Fact]
        public void FormatLogMessage_SubstitutesLogLevel()
        {
            var e = Make(level: LogLevel.Warning);
            string output = e.FormatLogMessage("{loglevel}");
            output.Should().Be("Warning");
        }

        [Fact]
        public void FormatLogMessage_SubstitutesThreadId()
        {
            var e = Make();
            string output = e.FormatLogMessage("{threadid}");
            output.Should().Be(e.ThreadId.ToString(CultureInfo.InvariantCulture));
        }

        [Fact]
        public void FormatLogMessage_SubstitutesHandle()
        {
            var e = Make(handle: "MyService");
            string output = e.FormatLogMessage("{handle}");
            output.Should().Be("MyService");
        }

        [Fact]
        public void FormatLogMessage_SubstitutesMessage()
        {
            var e = Make(message: "User did the thing");
            string output = e.FormatLogMessage("{message}");
            output.Should().Be("User did the thing");
        }

        [Fact]
        public void FormatLogMessage_SubstitutesColor()
        {
            var redColor = LogColor.FromRgb(255, 0, 0);
            var e = Make(color: redColor);
            string output = e.FormatLogMessage("{color}");
            output.Should().Be(redColor.ToString());
        }

        [Theory]
        [InlineData("{timestamp}")]
        [InlineData("{Timestamp}")]
        [InlineData("{TIMESTAMP}")]
        [InlineData("{tImEsTaMp}")]
        public void FormatLogMessage_PlaceholdersAreCaseInsensitive(string format)
        {
            var e = Make();
            string output = e.FormatLogMessage(format);
            output.Should().Be(e.LogDateTimeFormatted);
        }

        [Fact]
        public void FormatLogMessage_LiteralTextPreservesCasing()
        {
            var e = Make(handle: "MyService", message: "User did X");
            string output = e.FormatLogMessage("Time: {Timestamp} | Handle: {HANDLE} | Msg: {message}");

            // Literal text "Time:", "Handle:", "Msg:" must keep their casing.
            output.Should().StartWith("Time: ");
            output.Should().Contain(" | Handle: MyService");
            output.Should().Contain(" | Msg: User did X");
        }

        [Fact]
        public void FormatLogMessage_UnknownPlaceholder_LeftAsIs()
        {
            var e = Make();
            string output = e.FormatLogMessage("{message} [{notathing}]");
            output.Should().Be($"{e.LogText} [{{notathing}}]");
        }

        [Fact]
        public void FormatLogMessage_RepeatedPlaceholder_SubstitutedEachTime()
        {
            var e = Make(message: "X");
            string output = e.FormatLogMessage("{message}-{message}-{message}");
            output.Should().Be("X-X-X");
        }

        [Fact]
        public void FormatLogMessage_MultipleDistinctPlaceholders()
        {
            var e = Make(level: LogLevel.Error, handle: "Svc", message: "boom");
            string output = e.FormatLogMessage("[{loglevel}] {handle}: {message}");
            output.Should().Be($"[Error] Svc: boom");
        }

        // -------- ToString and GetLogMessageParts --------

        [Fact]
        public void ToString_ContainsHandleAndText()
        {
            var e = Make(handle: "Svc", message: "hello");
            string s = e.ToString();
            s.Should().Contain("Svc");
            s.Should().Contain("hello");
        }

        [Fact]
        public void GetLogMessageParts_ReturnsExpectedTuple()
        {
            var e = Make(handle: "Svc", message: "hello");
            var (timestamp, handle, body) = e.GetLogMessageParts();

            timestamp.Should().Be(e.LogDateTimeFormatted);
            handle.Should().Be("Svc");
            body.Should().Be("hello");
        }

        // -------- equality --------

        [Fact]
        public void Equals_UsesReferenceEquality()
        {
            var a = Make();
            var b = Make(); // same content, different instance
            a.Equals(b).Should().BeFalse();
            a.Equals(a).Should().BeTrue();
        }
    }
}
