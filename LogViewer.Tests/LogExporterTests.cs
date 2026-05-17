using System.Globalization;
using System.IO;
using CsvHelper;
using FluentAssertions;
using LogViewer;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Xunit;

namespace LogViewer.Tests
{
    [Collection(GlobalStateCollection.Name)]
    public class LogExporterTests
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

        // -------- Null guards --------

        [Fact]
        public async Task GetLogsAsJsonTextAsync_NullEvents_Throws()
        {
            await ((Func<Task>)(async () => await LogExporter.GetLogsAsJsonTextAsync(null!)))
                .Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task GetLogsAsTextAsync_NullEvents_Throws()
        {
            await ((Func<Task>)(async () => await LogExporter.GetLogsAsTextAsync(null!)))
                .Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task GetLogsAsCSVTextAsync_NullEvents_Throws()
        {
            await ((Func<Task>)(async () => await LogExporter.GetLogsAsCSVTextAsync(null!)))
                .Should().ThrowAsync<ArgumentNullException>();
        }

        // -------- JSON --------

        [Fact]
        public async Task Json_EmptyInput_ProducesEmptyArray()
        {
            var sb = await LogExporter.GetLogsAsJsonTextAsync(Array.Empty<LogEventArgs>());
            sb.ToString().Trim().Should().Be("[]");
        }

        [Fact]
        public async Task Json_SerializesPublicFields()
        {
            var e = Make(level: LogLevel.Warning, handle: "Svc", message: "boom");
            var sb = await LogExporter.GetLogsAsJsonTextAsync(new[] { e });

            using var doc = JsonDocument.Parse(sb.ToString());
            var root = doc.RootElement;
            root.GetArrayLength().Should().Be(1);
            var token = root[0];
            token.GetProperty("LogHandle").GetString().Should().Be("Svc");
            token.GetProperty("LogText").GetString().Should().Be("boom");
            // LogLevel serializes as string "Warning" with JsonStringEnumConverter.
            token.GetProperty("LogLevel").GetString().Should().Be("Warning");
        }

        [Fact]
        public async Task Json_OmitsJsonIgnoredProperties()
        {
            var e = Make();
            var sb = await LogExporter.GetLogsAsJsonTextAsync(new[] { e });
            string output = sb.ToString();

            // LogDateTimeFormatted is [JsonIgnore], must not appear.
            output.Should().NotContain("LogDateTimeFormatted");
        }

        [Fact]
        public async Task Json_IsIndented()
        {
            var e = Make();
            var sb = await LogExporter.GetLogsAsJsonTextAsync(new[] { e });
            string output = sb.ToString();

            output.Should().Contain(Environment.NewLine);
            output.Should().StartWith("[");
        }

        // -------- TXT --------

        [Fact]
        public async Task Text_EmptyInput_ProducesEmptyOutput()
        {
            var sb = await LogExporter.GetLogsAsTextAsync(Array.Empty<LogEventArgs>(), "{message}");
            sb.ToString().Should().BeEmpty();
        }

        [Fact]
        public async Task Text_AppliesFormatPerEvent_OneLineEach()
        {
            var events = new[]
            {
                Make(handle: "A", message: "first"),
                Make(handle: "B", message: "second"),
            };

            var sb = await LogExporter.GetLogsAsTextAsync(events, "{handle}: {message}");

            sb.ToString().Should().Be($"A: first{Environment.NewLine}B: second{Environment.NewLine}");
        }

        [Fact]
        public async Task Text_NullFormat_FallsBackToBaseLoggerExportFormat()
        {
            var events = new[] { Make(handle: "Svc", message: "boom") };

            var sb = await LogExporter.GetLogsAsTextAsync(events, null);
            string output = sb.ToString();

            output.Should().Contain("Svc");
            output.Should().Contain("boom");
        }

        [Fact]
        public async Task Text_PlaceholdersAreCaseInsensitive()
        {
            var events = new[] { Make(handle: "Svc", message: "boom") };

            var sb = await LogExporter.GetLogsAsTextAsync(events, "{Handle} | {MESSAGE}");
            sb.ToString().TrimEnd().Should().Be("Svc | boom");
        }

        // -------- CSV --------

        [Fact]
        public async Task Csv_EmitsHeaderRow()
        {
            var sb = await LogExporter.GetLogsAsCSVTextAsync(Array.Empty<LogEventArgs>());
            string output = sb.ToString();
            string firstLine = output.Split('\n')[0].TrimEnd('\r');

            firstLine.Should().Be("Timestamp,LogLevel,ThreadId,Color,Handle,Message");
        }

        [Fact]
        public async Task Csv_RoundTrip_PreservesSimpleMessage()
        {
            var events = new[] { Make(handle: "Svc", message: "hello world") };

            var sb = await LogExporter.GetLogsAsCSVTextAsync(events);
            var rows = ReadCsvRows(sb.ToString());

            rows.Should().HaveCount(1);
            rows[0]["Handle"].Should().Be("Svc");
            rows[0]["Message"].Should().Be("hello world");
        }

        [Fact]
        public async Task Csv_RoundTrip_PreservesEmbeddedQuotes()
        {
            // The v0.2.x #2 fix removed manual outer-quoting; CsvHelper now handles
            // escaping. Round-trip must yield the original string exactly.
            var events = new[] { Make(message: "He said \"hi\"") };

            var sb = await LogExporter.GetLogsAsCSVTextAsync(events);
            var rows = ReadCsvRows(sb.ToString());

            rows.Should().HaveCount(1);
            rows[0]["Message"].Should().Be("He said \"hi\"");
        }

        [Fact]
        public async Task Csv_RoundTrip_PreservesEmbeddedComma()
        {
            var events = new[] { Make(message: "a, b, c") };

            var sb = await LogExporter.GetLogsAsCSVTextAsync(events);
            var rows = ReadCsvRows(sb.ToString());

            rows[0]["Message"].Should().Be("a, b, c");
        }

        [Fact]
        public async Task Csv_NewlineInMessageIsSubstitutedAsToken()
        {
            // LogEventArgsMap replaces Environment.NewLine with the literal "{newline}"
            // marker so each event still occupies a single CSV row.
            var events = new[] { Make(message: $"line1{Environment.NewLine}line2") };

            var sb = await LogExporter.GetLogsAsCSVTextAsync(events);
            var rows = ReadCsvRows(sb.ToString());

            rows[0]["Message"].Should().Be("line1{newline}line2");
        }

        [Fact]
        public async Task Csv_RoundTrip_MultipleRows()
        {
            var events = new[]
            {
                Make(handle: "A", message: "first"),
                Make(handle: "B", message: "second"),
                Make(handle: "C", message: "third"),
            };

            var sb = await LogExporter.GetLogsAsCSVTextAsync(events);
            var rows = ReadCsvRows(sb.ToString());

            rows.Should().HaveCount(3);
            rows[0]["Handle"].Should().Be("A");
            rows[1]["Handle"].Should().Be("B");
            rows[2]["Handle"].Should().Be("C");
        }

        // -------- helpers --------

        private static List<Dictionary<string, string>> ReadCsvRows(string csv)
        {
            using var reader = new StringReader(csv);
            using var parser = new CsvReader(reader, CultureInfo.InvariantCulture);
            parser.Read();
            parser.ReadHeader();
            var headers = parser.HeaderRecord ?? [];

            var rows = new List<Dictionary<string, string>>();
            while (parser.Read())
            {
                var row = new Dictionary<string, string>();
                foreach (var h in headers)
                    row[h] = parser.GetField(h) ?? string.Empty;
                rows.Add(row);
            }
            return rows;
        }
    }
}
